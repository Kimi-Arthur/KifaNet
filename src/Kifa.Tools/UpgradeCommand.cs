using System.Reflection;
using CommandLine;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools;

[Verb("upgrade", HelpText = "Upgrade tool binary to the latest version.")]
public class UpgradeCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option("version", HelpText = "Target version to upgrade to.")]
    public string? Version { get; set; }

    [Option('p', "prerelease", HelpText = "Allow upgrading to prerelease versions.")]
    public bool Prerelease { get; set; } = false;

    public virtual string? PackageId => Assembly.GetEntryAssembly()?.GetName().Name;

    public virtual string? CurrentVersion
        => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
           ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

    public virtual ExecutionResult RunDotnetCommand(string arguments)
        => Executor.Run("dotnet", arguments);

    public override int Execute(KifaTask? task = null) {
        var packageId = PackageId;
        if (packageId == null) {
            Logger.Error("Unable to determine package ID for the current tool.");
            return 1;
        }

        ExecuteItem(packageId, () => UpgradeTool(packageId));
        return LogSummary();
    }

    KifaActionResult UpgradeTool(string packageId) {
        var arguments = $"tool update -g {packageId}";
        if (Version != null) {
            arguments += $" --version {Version}";
        }

        if (Prerelease) {
            arguments += " --prerelease";
        }

        var result = RunDotnetCommand(arguments);
        if (result.ExitCode != 0) {
            var error = result.StandardError.Trim();
            if (error.Length == 0) {
                error = result.StandardOutput.Trim();
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = error.Length > 0 ? error : $"Failed to upgrade {packageId} with exit code {result.ExitCode}."
            };
        }

        var output = result.StandardOutput.Trim();
        if (output.Contains("is already installed") || !output.Contains("was successfully updated")) {
            var message = output.Length > 0
                ? (CurrentVersion != null && !output.Contains(CurrentVersion)
                    ? $"{output} (Current version: {CurrentVersion})"
                    : output)
                : (CurrentVersion != null
                    ? $"Tool '{packageId}' (version '{CurrentVersion}') is already installed."
                    : $"Tool '{packageId}' is already installed.");

            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = message
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = output.Length > 0 ? output : $"Successfully upgraded {packageId}."
        };
    }
}
