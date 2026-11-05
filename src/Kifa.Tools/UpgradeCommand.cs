using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

    public virtual DateTime? GetVersionTime(string packageId, string version) {
        var entryLocation = Assembly.GetEntryAssembly()?.Location;
        if (entryLocation != null && string.Equals(Path.GetFileNameWithoutExtension(entryLocation),
                packageId, StringComparison.OrdinalIgnoreCase)) {
            if (version == CurrentVersion && File.Exists(entryLocation)) {
                return File.GetLastWriteTime(entryLocation);
            }

            if (CurrentVersion != null) {
                var candidatePath = entryLocation
                    .Replace($"/{CurrentVersion}/", $"/{version}/")
                    .Replace($"\\{CurrentVersion}\\", $"\\{version}\\");
                if (File.Exists(candidatePath)) {
                    return File.GetLastWriteTime(candidatePath);
                }
            }
        }

        var storeVersionDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools",
            ".store", packageId.ToLowerInvariant(), version.ToLowerInvariant());
        if (Directory.Exists(storeVersionDir)) {
            var dllPath = Directory
                .EnumerateFiles(storeVersionDir, "*.dll", SearchOption.AllDirectories)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), $"{packageId}.dll",
                    StringComparison.OrdinalIgnoreCase));
            if (dllPath != null) {
                return File.GetLastWriteTime(dllPath);
            }
        }

        return null;
    }

    public override int Execute(KifaTask? task = null) {
        var packageId = PackageId;
        if (packageId == null) {
            Logger.Error("Unable to determine package ID for the current tool.");
            return 1;
        }

        ExecuteItem(packageId, () => UpgradeTool(packageId));
        return LogSummary();
    }

    const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
    static readonly Regex VersionPattern = new(@"version '(?<version>[^']+)'(?<closingParen>\))?", RegexOptions.Compiled);

    KifaActionResult UpgradeTool(string packageId) {
        var currentVersion = CurrentVersion;
        var currentVersionTime = currentVersion != null ? GetVersionTime(packageId, currentVersion) : null;

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

        var rawOutput = result.StandardOutput.Trim();
        var output = FormatOutputWithTimestamps(packageId, rawOutput, currentVersion, currentVersionTime);

        if (rawOutput.Contains("is already installed") || !rawOutput.Contains("was successfully updated")) {
            var currentVersionLabel = currentVersion != null
                ? (currentVersionTime != null
                    ? $"{currentVersion}, {currentVersionTime.Value.ToString(TimeFormat)}"
                    : currentVersion)
                : null;

            var message = output.Length > 0
                ? (currentVersion != null && !rawOutput.Contains(currentVersion)
                    ? $"{output} (Current version: {currentVersionLabel})"
                    : output)
                : (currentVersion != null
                    ? $"Tool '{packageId}' (version '{currentVersion}'{(currentVersionTime != null ? $", {currentVersionTime.Value.ToString(TimeFormat)}" : "")}) is already installed."
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

    string FormatOutputWithTimestamps(string packageId, string output, string? currentVersion, DateTime? currentVersionTime)
        => VersionPattern.Replace(output, match => {
            var version = match.Groups["version"].Value;
            var time = version == currentVersion && currentVersionTime != null
                ? currentVersionTime
                : GetVersionTime(packageId, version);
            if (time == null) {
                return match.Value;
            }

            var formattedTime = time.Value.ToString(TimeFormat);
            return match.Groups["closingParen"].Success
                ? $"version '{version}', {formattedTime})"
                : $"version '{version}' ({formattedTime})";
        });
}
