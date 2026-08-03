using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CommandLine;
using GlobExpressions;
using Kifa.Service;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static Dictionary<string, bool> alwaysDefaultForSelectOne = new();
    static Dictionary<string, int> defaultIndexForSelectOne = new();

    // Matches single choice prompt input: ^[index][p<part>][flags]$
    // Group 1: Index (e.g. 3). Empty falls back to default choice index.
    // Group 2: Sub-part number after literal 'p' (e.g. 4 in 3p4 for split episodes).
    // Group 3: Flags - 'a' (always choose same index), 's' (special handling), 'i' (ignore).
    // Examples: "" (default), "3" (choice 3), "3p4" (choice 3 part 4), "3s" (choice 3 special), "3a" (always choice 3).
    static readonly Regex SingleChoiceRegex = new(@"^(\d*)(?:p(\d+))?([asi]*)$");

    [Option('y', "yes",
        HelpText = "Always yes to all confirmations with default value (not always yes).")]
    public bool AutoConfirmDefault { get; set; } = false;

    public KifaActionResult<(TChoice Choice, int? Part, int Index, bool Special)>
        SelectOne<TChoice>(List<TChoice> choices, Func<TChoice, string>? choiceToString = null,
            string? choiceName = null, int startingIndex = 0, string? specialHelpText = null,
            string? partHelpText = null, bool reverse = false, string? selectionKey = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0) {
        choiceName ??= "items";
        selectionKey = string.IsNullOrEmpty(selectionKey)
            ? $"{callerFilePath}:{callerLineNumber}"
            : selectionKey;

        if (choices.Count == 0) {
            Logger.Debug($"No {choiceName} available to select from.");
            return KifaActionResult<(TChoice Choice, int? Part, int Index, bool Special)>.Warning(
                $"No {choiceName} available to select from.");
        }

        defaultIndexForSelectOne.TryAdd(selectionKey, 0);
        alwaysDefaultForSelectOne.TryAdd(selectionKey, AutoConfirmDefault);

        var choiceStrings = choiceToString == null
            ? choices.Select(c => c.ToString()).ToList()
            : choices.Select(choiceToString).ToList();

        if (reverse) {
            for (var i = choices.Count - 1; i >= 0; i--) {
                Console.WriteLine($"[{i + startingIndex}]\t{choiceStrings[i]}");
            }
        } else {
            for (var i = 0; i < choices.Count; i++) {
                Console.WriteLine($"[{i + startingIndex}]\t{choiceStrings[i]}");
            }
        }

        if (defaultIndexForSelectOne[selectionKey] >= choices.Count) {
            defaultIndexForSelectOne[selectionKey] = 0;

            // Cancel alwaysDefault when the value is updated.
            alwaysDefaultForSelectOne[selectionKey] = false;
        }

        var defaultIndex = defaultIndexForSelectOne[selectionKey];

        Console.WriteLine(
            $"\nDefault [{defaultIndex + startingIndex}]: {choiceStrings[defaultIndex]}\n");

        if (alwaysDefaultForSelectOne[selectionKey]) {
            Console.WriteLine(
                $"Automatically chose [{defaultIndex + startingIndex}] as previously instructed.\n");
            return (choices[defaultIndex], null, defaultIndex, false);
        }

        Console.WriteLine(
            $"Choose one from the {choiceName} above [{startingIndex} - {choices.Count - 1 + startingIndex}].");
        Console.WriteLine("Append 'a' to always choose the same index,");
        if (specialHelpText != null) {
            Console.WriteLine($"Append 's' {specialHelpText},");
        }

        if (partHelpText != null) {
            Console.WriteLine($"Use '<index>p<part>' {partHelpText},");
        }

        Console.Write($"Default is [{defaultIndex + startingIndex}]: ");
        var match = SingleChoiceRegex.Match(Console.ReadLine() ?? "");
        while (!match.Success) {
            Console.WriteLine("Invalid choice. Try again:");
            Console.Write($"Default is [{defaultIndex + startingIndex}]: ");
            match = SingleChoiceRegex.Match(Console.ReadLine() ?? "");
        }

        var choiceText = match.Groups[1].Value;
        var partText = match.Groups[2].Value;
        int? part = string.IsNullOrEmpty(partText) ? null : int.Parse(partText);

        var chosenIndex = string.IsNullOrEmpty(choiceText)
            ? defaultIndex
            : int.Parse(choiceText) - startingIndex;

        var flags = match.Groups[3].Value;

        if (flags.Contains('i')) {
            return KifaActionResult<(TChoice Choice, int? Part, int Index, bool Special)>.Skipped(
                "Ignored by user.");
        }

        if (flags.Contains('a')) {
            alwaysDefaultForSelectOne[selectionKey] = true;
        }

        var special = flags.Contains('s');
        if (specialHelpText == null && special) {
            throw new InvalidChoiceException("Special is not supported...");
        }

        if (partHelpText == null && part.HasValue) {
            throw new InvalidChoiceException("Part selection is not supported...");
        }

        defaultIndexForSelectOne[selectionKey] = chosenIndex;
        if (chosenIndex < 0 || chosenIndex >= choices.Count) {
            throw new InvalidChoiceException($"Choice {chosenIndex} is out of range.");
        }

        return (choices[chosenIndex], part, chosenIndex, special);
    }

    static readonly Dictionary<string, string> DefaultReplyForSelectMany = new();
    static readonly Dictionary<string, bool> AlwaysDefaultForSelectMany = new();

    public KifaActionResult<List<TChoice>> SelectMany<TChoice>(List<TChoice> choices,
        Func<TChoice, string> choiceItemString,
        FuncOrValue<List<TChoice>, string>? choiceSummaryString = null, int startingIndex = 0,
        string? selectionKey = null, bool skipIfEmpty = true, bool reverse = false,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0) {
        selectionKey = string.IsNullOrEmpty(selectionKey)
            ? $"{callerFilePath}:{callerLineNumber}"
            : selectionKey;
        if (choices.Count == 0) {
            return new KifaActionResult<List<TChoice>>(
                skipIfEmpty ? KifaActionStatus.Skipped : KifaActionStatus.Warning,
                $"No {choiceSummaryString?.Get(choices) ?? "items"} available to select from.");
        }

        AlwaysDefaultForSelectMany.TryAdd(selectionKey, AutoConfirmDefault);
        DefaultReplyForSelectMany.TryAdd(selectionKey, "");

        var chosenIndexes = Enumerable.Range(0, choices.Count).ToList();

        while (true) {
            var selectedChoices = chosenIndexes.Select(index => choices[index]).ToList();
            if (reverse) {
                for (var i = selectedChoices.Count - 1; i >= 0; i--) {
                    Console.WriteLine(
                        $"[{i + startingIndex}]\t{choiceItemString(selectedChoices[i])}");
                }
            } else {
                for (var i = 0; i < selectedChoices.Count; i++) {
                    Console.WriteLine(
                        $"[{i + startingIndex}]\t{choiceItemString(selectedChoices[i])}");
                }
            }

            var line = "";

            if (AlwaysDefaultForSelectMany[selectionKey]) {
                line = DefaultReplyForSelectMany[selectionKey];
                Console.WriteLine($"Automatically chose [{line}] as previously instructed.\n");
            } else {
                var messages = new[] {
                    $"Hint: Default for all, prefix 'a' for always choice, prefix '^' for invert, '-' for inclusive range, ',' for combination, eg '{startingIndex}' '-{startingIndex + 3}' '^{startingIndex + 2}'.",
                    "      '?' to restart, '/<glob>' (e.g. '/*EP[0-9]*.mp4') or '^/<glob>' to include or exclude choices",
                    $"Select 0 or more from the above {selectedChoices.Count} {choiceSummaryString?.Get(selectedChoices) ?? "items"} [{startingIndex}-{startingIndex + selectedChoices.Count - 1}]: "
                };

                Console.Write(messages.JoinBy("\n"));
                line = Console.ReadLine() ?? "";
            }

            if (line == "?") {
                chosenIndexes = Enumerable.Range(0, choices.Count).ToList();
                continue;
            }

            var flags = "";
            if (line.StartsWith('a')) {
                flags = "a";
                line = line[1..];
            }

            if (line == "") {
                if (flags.Contains('a')) {
                    // Only used when alwaysDefault is true. Otherwise, all is always the default.
                    DefaultReplyForSelectMany[selectionKey] = line;
                    AlwaysDefaultForSelectMany[selectionKey] = true;
                }

                Logger.Debug(
                    $"Selected {chosenIndexes.Count} {choiceSummaryString?.Get(selectedChoices) ?? "items"} above.");
                return selectedChoices;
            }

            if (line == "^") {
                chosenIndexes = [];
                return KifaActionResult<List<TChoice>>.Skipped("Deselected by user.");
            }

            try {
                var newIndexes = ParseSelection(line, selectedChoices, choiceItemString, startingIndex);
                chosenIndexes = newIndexes.Select(i => chosenIndexes[i]).ToList();
                if (flags.Contains('a') || AlwaysDefaultForSelectMany[selectionKey]) {
                    DefaultReplyForSelectMany[selectionKey] = line;
                    AlwaysDefaultForSelectMany[selectionKey] = true;
                    return chosenIndexes.Select(i => choices[i]).ToList();
                }
            } catch (Exception) {
                Console.WriteLine("Invalid choice. Try again:");
            }
        }
    }

    public static List<int> ParseSelection<TChoice>(string line, List<TChoice> selectedChoices,
        Func<TChoice, string> choiceItemString, int startingIndex = 0) {
        var tokens = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        HashSet<int>? currentSelection = null;

        foreach (var rawToken in tokens) {
            var token = rawToken;
            var excluded = token.StartsWith('^');
            if (excluded) {
                token = token[1..];
            }

            List<int> matchingIndices = new();

            if (token.StartsWith('/')) {
                var glob = new Glob(token[1..]);
                for (var i = 0; i < selectedChoices.Count; i++) {
                    if (glob.IsMatch(choiceItemString(selectedChoices[i]))) {
                        matchingIndices.Add(i);
                    }
                }
            } else {
                int rangeStart, rangeEnd;
                if (token.Contains('-')) {
                    var parts = token.Split('-');
                    rangeStart = parts[0].Length > 0 ? int.Parse(parts[0]) - startingIndex : 0;
                    rangeEnd = parts[1].Length > 0
                        ? int.Parse(parts[1]) - startingIndex + 1
                        : selectedChoices.Count;
                } else {
                    rangeStart = int.Parse(token) - startingIndex;
                    rangeEnd = rangeStart + 1;
                }

                if (rangeStart < 0 || rangeEnd < 0 || rangeStart > selectedChoices.Count ||
                    rangeEnd > selectedChoices.Count || rangeStart >= rangeEnd) {
                    throw new ArgumentOutOfRangeException(nameof(line), "Index out of range.");
                }

                for (var i = rangeStart; i < rangeEnd; i++) {
                    matchingIndices.Add(i);
                }
            }

            if (currentSelection == null) {
                if (excluded) {
                    currentSelection =
                        new HashSet<int>(Enumerable.Range(0, selectedChoices.Count));
                    currentSelection.ExceptWith(matchingIndices);
                } else {
                    currentSelection = new HashSet<int>(matchingIndices);
                }
            } else {
                if (excluded) {
                    currentSelection.ExceptWith(matchingIndices);
                } else {
                    currentSelection.UnionWith(matchingIndices);
                }
            }
        }

        if (currentSelection == null) {
            throw new InvalidOperationException("No valid selection tokens.");
        }

        return currentSelection.OrderBy(x => x).ToList();
    }

    public string? Confirm(string prefix, string suggested,
        Func<string, string?>? validation = null) {
        while (true) {
            if (validation == null) {
                Console.WriteLine($"{prefix}\n\n{suggested}");
            } else {
                Console.WriteLine($"{prefix}\n\n{suggested} ({validation(suggested) ?? "OK"})");
            }

            var line = Console.ReadLine() ?? "";
            if (line == "") {
                var validationResult = validation?.Invoke(suggested);
                if (validationResult != null) {
                    Console.WriteLine(
                        $"Current value {suggested} is invalid, will return null instead: {validationResult}");
                    return null;
                }

                return suggested;
            }

            suggested = line;
        }
    }

    public bool Confirm(string prefix, bool suggested = true) {
        if (AutoConfirmDefault) {
            Logger.Debug($"Auto selected default {suggested} as enabled by -y or --yes.");
            return suggested;
        }

        while (true) {
            var suggestedOptions = suggested ? "Y/n" : "y/N";
            Console.Write($"{prefix} [{suggestedOptions}]?");

            var line = Console.ReadLine()!;
            if (line == "") {
                return suggested;
            }

            if (line.ToLower().StartsWith("y")) {
                return true;
            }

            if (line.ToLower().StartsWith("n")) {
                return false;
            }
        }
    }
}
