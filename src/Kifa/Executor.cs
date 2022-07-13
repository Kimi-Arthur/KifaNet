using System.Diagnostics;
using NLog;

namespace Kifa;

public static class Executor {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static ExecutionResult Run(string command, string arguments) {
        Logger.Trace($"Executing: {command} {arguments}");
        using var proc = new Process {
            StartInfo = {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false
            }
        };

        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();
        proc.WaitForExit();
        var result = new ExecutionResult {
            ExitCode = proc.ExitCode,
            StandardOutput = proc.StandardOutput.ReadToEnd(),
            StandardError = proc.StandardError.ReadToEnd()
        };

        var logLevel = result.ExitCode == 0 ? LogLevel.Trace : LogLevel.Warn;
        Logger.Log(logLevel, $"exit code: {result.ExitCode}");
        Logger.Log(logLevel, $"stdout: {result.StandardOutput}");
        Logger.Log(logLevel, $"stderr: {result.StandardError}");

        return result;
    }
}

public class ExecutionResult {
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = "";
    public string StandardError { get; set; } = "";
}
