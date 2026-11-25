using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Kifa.AI;

// Shells out to the local Antigravity CLI (`agy -p`) with optional --json-schema structured output.
public class AgyLlmClient : LlmClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string DefaultExecutable { get; set; } = "agy";
    public static string DefaultModel { get; set; } = "gemini-3.8-flash-low";
    public static string? DefaultEffort { get; set; }

    public string Executable { get; set; } = DefaultExecutable;
    public string Model { get; set; } = DefaultModel;
    public string? Effort { get; set; } = DefaultEffort;
    public int TimeoutSeconds { get; set; } = 60;

    readonly Func<ProcessStartInfo, (int ExitCode, string StdOut, string StdErr)>? processRunner;

    public AgyLlmClient(
        Func<ProcessStartInfo, (int ExitCode, string StdOut, string StdErr)>? processRunner = null) {
        this.processRunner = processRunner;
    }

    public LlmResponse? Ask(string prompt, string? jsonSchema = null, string? systemPrompt = null) {
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(Executable)) {
            return null;
        }

        var fullPrompt = string.IsNullOrWhiteSpace(systemPrompt)
            ? prompt
            : $"{systemPrompt}\n\n{prompt}";

        var psi = new ProcessStartInfo {
            FileName = Executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("--disable-slash-commands");
        psi.ArgumentList.Add("--output-format");
        psi.ArgumentList.Add("json");
        if (!string.IsNullOrWhiteSpace(jsonSchema)) {
            psi.ArgumentList.Add("--json-schema");
            psi.ArgumentList.Add(jsonSchema);
        }

        if (!string.IsNullOrWhiteSpace(Effort)) {
            psi.ArgumentList.Add("--effort");
            psi.ArgumentList.Add(Effort);
        }

        if (!string.IsNullOrWhiteSpace(Model)) {
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(Model);
        }

        if (TimeoutSeconds > 0) {
            psi.ArgumentList.Add("--print-timeout");
            psi.ArgumentList.Add($"{TimeoutSeconds}s");
        }

        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add(fullPrompt);

        try {
            var (exitCode, stdout, stderr) = processRunner != null
                ? processRunner(psi)
                : RunProcess(psi, TimeoutSeconds);
            if (exitCode != 0) {
                Logger.Warn($"AgyLlmClient exited with {exitCode}: {stderr}");
                return null;
            }

            var content = UnwrapEnvelope(stdout);
            if (string.IsNullOrWhiteSpace(content)) {
                return null;
            }

            var modelTag = string.IsNullOrWhiteSpace(Model) ? "default" : Model;
            if (!string.IsNullOrWhiteSpace(Effort)) {
                modelTag = $"{modelTag}:{Effort}";
            }

            return new LlmResponse(content, $"agy:{modelTag}");
        } catch (Exception ex) {
            Logger.Warn(ex, "AgyLlmClient error.");
            return null;
        }
    }

    static string? UnwrapEnvelope(string? stdout) {
        if (string.IsNullOrWhiteSpace(stdout)) {
            return null;
        }

        var trimmed = stdout.Trim();
        try {
            if (JToken.Parse(trimmed) is JObject obj) {
                if (string.Equals(obj["status"]?.Value<string>(), "ERROR",
                        StringComparison.OrdinalIgnoreCase)) {
                    Logger.Warn($"AgyLlmClient error response: {obj["error"]?.Value<string>()}");
                    return null;
                }

                var structured = obj["structured_output"];
                if (structured != null && structured.Type != JTokenType.Null) {
                    return structured.ToString(Formatting.None);
                }

                var response = obj["response"]?.Value<string>() ?? obj["result"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(response)) {
                    return response.Trim();
                }
            }
        } catch (JsonException) {
            // Raw text output.
        }

        return trimmed;
    }

    static (int ExitCode, string StdOut, string StdErr) RunProcess(ProcessStartInfo psi,
        int timeoutSeconds) {
        using var process = Process.Start(psi) ??
                            throw new InvalidOperationException(
                                $"Failed to start process '{psi.FileName}'.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var timeoutMs = timeoutSeconds > 0 ? (timeoutSeconds + 10) * 1000 : 120_000;
        if (!process.WaitForExit(timeoutMs)) {
            try {
                process.Kill(entireProcessTree: true);
            } catch {
                // Best effort cleanup on timeout.
            }

            return (-1, "", $"Timed out after {timeoutSeconds}s.");
        }

        return (process.ExitCode, stdoutTask.GetAwaiter().GetResult(),
            stderrTask.GetAwaiter().GetResult());
    }
}
