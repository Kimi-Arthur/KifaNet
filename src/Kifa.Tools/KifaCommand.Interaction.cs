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
    // Matches single choice prompt input: ^([a]?)(\d*)(?:p(\d+))?(s?)$
    // Group 1: Prefix flag 'a' (always choose same index).
    // Group 2: Index (e.g. 3). Empty falls back to default choice index.
    // Group 3: Sub-part number after literal 'p' (e.g. 4 in 3p4 for split episodes).
    // Group 4: Suffix flag 's' (special handling).
    // Examples: "" (default), "3" (choice 3), "a" or "a3" (always choice), "3p4" (choice 3 part 4), "3s" (choice 3 special), "^" (ignore).
    static readonly Regex SingleChoiceRegex = new(@"^([a]?)(\d*)(?:p(\d+))?(s?)$");

    static readonly Dictionary<string, bool> AlwaysDefaultForSelectOne = new();
    static readonly Dictionary<string, int> DefaultIndexForSelectOne = new();

    [Option('y', "yes",
        HelpText = "Always yes to all confirmations with default value (not always yes).")]
    public bool AutoConfirmDefault { get; set; } = false;

    public KifaActionResult<(TChoice Choice, int? Part, int Index, bool Special)>
        SelectOne<TChoice>(List<TChoice> choices, Func<TChoice, string>? choiceToString = null,
            string? choiceName = null, int startingIndex = 1, string? specialHelpText = null,
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

        DefaultIndexForSelectOne.TryAdd(selectionKey, 0);
        AlwaysDefaultForSelectOne.TryAdd(selectionKey, AutoConfirmDefault);

        var choiceStrings = choiceToString == null
            ? choices.Select(c => c?.ToString() ?? "").ToList()
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

        if (DefaultIndexForSelectOne[selectionKey] >= choices.Count) {
            DefaultIndexForSelectOne[selectionKey] = 0;

            // Cancel alwaysDefault when the value is updated.
            AlwaysDefaultForSelectOne[selectionKey] = false;
        }

        var defaultIndex = DefaultIndexForSelectOne[selectionKey];

        if (AlwaysDefaultForSelectOne[selectionKey]) {
            Console.WriteLine(
                $"Automatically chose [{defaultIndex + startingIndex}] as previously instructed.\n");
            return (choices[defaultIndex], null, defaultIndex, false);
        }

        Console.WriteLine();
        var messages = new List<string> {
            $"Choose one from the {choiceName} above [{startingIndex} - {choices.Count - 1 + startingIndex}].",
            "Hint: Prefix 'a' to always choose, '^' to ignore."
        };

        if (specialHelpText != null) {
            messages.Add($"\t's' {specialHelpText}.");
        }

        if (partHelpText != null) {
            messages.Add($"\t'<index>p<part>' {partHelpText}.");
        }

        messages.Add($"Default is [{defaultIndex + startingIndex}] ({choiceStrings[defaultIndex]}): ");
        Console.Write(messages.JoinBy("\n"));

        while (true) {
            var rawLine = (Console.ReadLine() ?? "").Trim();
            if (rawLine == "^") {
                return KifaActionResult<(TChoice Choice, int? Part, int Index, bool Special)>
                    .Skipped("Ignored by user.");
            }

            var match = SingleChoiceRegex.Match(rawLine);
            if (!match.Success) {
                Console.WriteLine("Invalid choice. Try again:");
                Console.Write(
                    $"Default is [{defaultIndex + startingIndex}] ({choiceStrings[defaultIndex]}): ");
                continue;
            }

            var always = match.Groups[1].Value == "a";
            var choiceText = match.Groups[2].Value;
            var partText = match.Groups[3].Value;
            var special = match.Groups[4].Value == "s";

            int? part = string.IsNullOrEmpty(partText) ? null : int.Parse(partText);
            var chosenIndex = string.IsNullOrEmpty(choiceText)
                ? defaultIndex
                : int.Parse(choiceText) - startingIndex;

            if (chosenIndex < 0 || chosenIndex >= choices.Count) {
                Console.WriteLine("Invalid choice. Try again:");
                Console.Write(
                    $"Default is [{defaultIndex + startingIndex}] ({choiceStrings[defaultIndex]}): ");
                continue;
            }

            if (specialHelpText == null && special) {
                Console.WriteLine("Special is not supported. Try again:");
                Console.Write(
                    $"Default is [{defaultIndex + startingIndex}] ({choiceStrings[defaultIndex]}): ");
                continue;
            }

            if (partHelpText == null && part.HasValue) {
                Console.WriteLine("Part selection is not supported. Try again:");
                Console.Write(
                    $"Default is [{defaultIndex + startingIndex}] ({choiceStrings[defaultIndex]}): ");
                continue;
            }

            if (always) {
                AlwaysDefaultForSelectOne[selectionKey] = true;
            }

            DefaultIndexForSelectOne[selectionKey] = chosenIndex;
            return (choices[chosenIndex], part, chosenIndex, special);
        }
    }

    static readonly Dictionary<string, string> DefaultReplyForSelectMany = new();
    static readonly Dictionary<string, bool> AlwaysDefaultForSelectMany = new();

    public KifaActionResult<List<TChoice>> SelectMany<TChoice>(List<TChoice> choices,
        Func<TChoice, string>? choiceToString = null,
        FuncOrValue<List<TChoice>, string>? choiceSummaryString = null, int startingIndex = 1,
        string? selectionKey = null, bool skipIfEmpty = true, bool reverse = false,
        [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0) {
        var choiceItemString = choiceToString ?? (c => c?.ToString() ?? "");
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

        if (!string.IsNullOrEmpty(DefaultReplyForSelectMany[selectionKey])) {
            try {
                ParseSelection(DefaultReplyForSelectMany[selectionKey], choices, choiceItemString,
                    startingIndex);
            } catch {
                DefaultReplyForSelectMany[selectionKey] = "";
                AlwaysDefaultForSelectMany[selectionKey] = false;
            }
        }

        var defaultReply = DefaultReplyForSelectMany[selectionKey];
        var defaultDisplay = string.IsNullOrEmpty(defaultReply) ? "all" : defaultReply;

        if (AlwaysDefaultForSelectMany[selectionKey]) {
            Console.WriteLine(
                $"Automatically chose [{defaultDisplay}] as previously instructed.\n");
            if (string.IsNullOrEmpty(defaultReply)) {
                return choices;
            }

            var initialIndexes = ParseSelection(defaultReply, choices, choiceItemString,
                startingIndex);
            return initialIndexes.Select(i => choices[i]).ToList();
        }

        var chosenIndexes = Enumerable.Range(0, choices.Count).ToList();
        var isFirstPrompt = true;
        var lastSelectionString = defaultReply;

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

            Console.WriteLine();
            if (isFirstPrompt) {
                var defaultCountSummary = string.IsNullOrEmpty(defaultReply)
                    ? $"{choices.Count} items"
                    : $"{ParseSelection(defaultReply, choices, choiceItemString, startingIndex).Count} items";

                var messages = new[] {
                    $"Choose 0 or more from the {choiceSummaryString?.Get(selectedChoices) ?? "items"} above [{startingIndex} - {selectedChoices.Count - 1 + startingIndex}].",
                    $"Hint: Prefix 'a' to always choose, prefix '^' to invert, '-' for inclusive range, ',' for combination (e.g. '{startingIndex}', '-{startingIndex + 3}', '^{startingIndex + 2}').",
                    "\t'?' to restart, '*' or 'all' for all items, '/<glob>' (e.g. '/*EP[0-9]*.mp4') or '^/<glob>' to include or exclude choices, '^' to ignore.",
                    $"Default is [{defaultDisplay}] ({defaultCountSummary}): "
                };

                Console.Write(messages.JoinBy("\n"));
            } else {
                var countText = $"{selectedChoices.Count} {choiceSummaryString?.Get(selectedChoices) ?? "items"} selected";
                var rangeText = selectedChoices.Count > 0
                    ? $" [{startingIndex} - {selectedChoices.Count - 1 + startingIndex}]"
                    : "";
                Console.Write(
                    $"{countText}{rangeText}. Press Enter to confirm, or enter further filter ('?' to restart, '^' to cancel): ");
            }

            var line = (Console.ReadLine() ?? "").Trim();

            if (line == "?") {
                chosenIndexes = Enumerable.Range(0, choices.Count).ToList();
                isFirstPrompt = true;
                lastSelectionString = defaultReply;
                continue;
            }

            var flags = "";
            if (line.StartsWith('a') && line != "all") {
                flags = "a";
                line = line[1..].Trim();
            }

            if (line == "^") {
                chosenIndexes = [];
                return KifaActionResult<List<TChoice>>.Skipped("Ignored by user.");
            }

            if (line == "*" || line.Equals("all", StringComparison.OrdinalIgnoreCase)) {
                DefaultReplyForSelectMany[selectionKey] = "";
                if (flags.Contains('a')) {
                    AlwaysDefaultForSelectMany[selectionKey] = true;
                }

                Logger.Debug(
                    $"Selected {choices.Count} {choiceSummaryString?.Get(choices) ?? "items"} above.");
                return choices;
            }

            if (line == "") {
                if (flags.Contains('a')) {
                    AlwaysDefaultForSelectMany[selectionKey] = true;
                }

                if (isFirstPrompt) {
                    if (string.IsNullOrEmpty(defaultReply)) {
                        DefaultReplyForSelectMany[selectionKey] = "";
                        Logger.Debug(
                            $"Selected {chosenIndexes.Count} {choiceSummaryString?.Get(selectedChoices) ?? "items"} above.");
                        return selectedChoices;
                    }

                    try {
                        var defaultIndexes = ParseSelection(defaultReply, choices, choiceItemString,
                            startingIndex);
                        DefaultReplyForSelectMany[selectionKey] = defaultReply;
                        Logger.Debug(
                            $"Selected {defaultIndexes.Count} {choiceSummaryString?.Get(choices) ?? "items"} above.");
                        return defaultIndexes.Select(i => choices[i]).ToList();
                    } catch {
                        Console.WriteLine("Invalid default choice. Resetting to all:");
                        DefaultReplyForSelectMany[selectionKey] = "";
                        defaultReply = "";
                        defaultDisplay = "all";
                        chosenIndexes = Enumerable.Range(0, choices.Count).ToList();
                        continue;
                    }
                }

                DefaultReplyForSelectMany[selectionKey] = lastSelectionString;
                Logger.Debug(
                    $"Selected {chosenIndexes.Count} {choiceSummaryString?.Get(selectedChoices) ?? "items"} above.");
                return selectedChoices;
            }

            try {
                var newIndexes =
                    ParseSelection(line, selectedChoices, choiceItemString, startingIndex);
                chosenIndexes = newIndexes.Select(i => chosenIndexes[i]).ToList();
                isFirstPrompt = false;
                lastSelectionString = line;
                DefaultReplyForSelectMany[selectionKey] = line;
                if (flags.Contains('a') || AlwaysDefaultForSelectMany[selectionKey]) {
                    return chosenIndexes.Select(i => choices[i]).ToList();
                }
            } catch (Exception) {
                Console.WriteLine("Invalid choice. Try again:");
            }
        }
    }

    public static List<int> ParseSelection<TChoice>(string line, List<TChoice> selectedChoices,
        Func<TChoice, string> choiceItemString, int startingIndex = 1) {
        var tokens = line.Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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
                    currentSelection = new HashSet<int>(Enumerable.Range(0, selectedChoices.Count));
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
