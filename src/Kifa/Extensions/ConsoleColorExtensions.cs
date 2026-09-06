using System;
using System.Diagnostics.CodeAnalysis;

namespace Kifa;

public static class ConsoleColorExtensions {
    public static bool? ForceEnabled { get; set; }

    public static bool Enabled =>
        ForceEnabled ?? (!Console.IsOutputRedirected &&
                        Environment.GetEnvironmentVariable("NO_COLOR") == null);

    const string ResetCode = "\e[0m";

    [return: NotNullIfNotNull(nameof(text))]
    static string? Color(this string? text, int code) {
        if (text == null || text.Length == 0 || !Enabled) {
            return text;
        }

        var colorCode = $"\e[{code}m";
        var content = text.Contains(ResetCode)
            ? text.Replace(ResetCode, $"{ResetCode}{colorCode}")
            : text;
        return $"{colorCode}{content}{ResetCode}";
    }

    const int TraceCode = 37; // Gray

    [return: NotNullIfNotNull(nameof(text))]
    public static string? Trace(this string? text) => text.Color(TraceCode);

    const int DebugCode = 97; // White

    [return: NotNullIfNotNull(nameof(text))]
    public static string? Debug(this string? text) => text.Color(DebugCode);

    const int InfoCode = 32; // Green

    [return: NotNullIfNotNull(nameof(text))]
    public static string? Info(this string? text) => text.Color(InfoCode);

    const int WarnCode = 33; // Yellow

    [return: NotNullIfNotNull(nameof(text))]
    public static string? Warn(this string? text) => text.Color(WarnCode);

    const int ErrorCode = 35; // Magenta

    [return: NotNullIfNotNull(nameof(text))]
    public static string? Error(this string? text) => text.Color(ErrorCode);

    const int FatalCode = 31; // Red

    [return: NotNullIfNotNull(nameof(text))]
    public static string? Fatal(this string? text) => text.Color(FatalCode);
}
