using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using GlobExpressions;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static Dictionary<string, bool> alwaysDefaultForSelectOne = new();
    static Dictionary<string, int> defaultIndexForSelectOne = new();

    static readonly Regex SingleChoiceRegex = new(@"^(\d*)([asi]*)$");

    [Option('y', "yes",
        HelpText = "Always yes to all confirmations with default value (not always yes).")]
    public bool AutoConfirmDefault { get; set; } = false;

    public (TChoice Choice, int Index, bool Special)? SelectOne<TChoice>(List<TChoice> choices,
        Func<TChoice, string>? choiceToString = null, string? choiceName = null,
        int startingIndex = 0, bool supportsSpecial = false, bool reverse = false,
        string selectionKey = "") {
        defaultIndexForSelectOne.TryAdd(selectionKey, 0);
        alwaysDefaultForSelectOne.TryAdd(selectionKey, false);

        var choiceStrings = choiceToString == null
            ? choices.Select(c => c.ToString()).ToList()
            : choices.Select(choiceToString).ToList();

        choiceName ??= "items";

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
            return (choices[defaultIndex], defaultIndex, false);
        }

        Console.WriteLine(
            $"Choose one from the {choiceName} above [{startingIndex} - {choices.Count - 1 + startingIndex}].");
        Console.WriteLine("Append 'a' to always choose the same index,");
        if (supportsSpecial) {
            Console.WriteLine("Append 's' to use special handling,");
        }

        Console.Write($"Default is [{defaultIndex + startingIndex}]: ");
        var match = SingleChoiceRegex.Match(Console.ReadLine() ?? "");
        while (!match.Success) {
            Console.WriteLine("Invalid choice. Try again:");
            Console.Write($"Default is [{defaultIndex + startingIndex}]: ");
            match = SingleChoiceRegex.Match(Console.ReadLine() ?? "");
        }

        var choiceText = match.Groups[1].Value;
        var chosenIndex = string.IsNullOrEmpty(choiceText)
            ? defaultIndex
            : int.Parse(choiceText) - startingIndex;

        var flags = match.Groups[2].Value;

        if (flags.Contains('i')) {
            return null;
        }

        if (flags.Contains('a')) {
            alwaysDefaultForSelectOne[selectionKey] = true;
        }

        var special = flags.Contains('s');
        if (!supportsSpecial && special) {
            throw new InvalidChoiceException("Special is not supported...");
        }

        defaultIndexForSelectOne[selectionKey] = chosenIndex;
        if (chosenIndex < 0 || chosenIndex >= choices.Count) {
            throw new InvalidChoiceException($"Choice {chosenIndex} is out of range.");
        }

        return (choices[chosenIndex], chosenIndex, special);
    }

    static readonly Regex ManyChoiceRegex = new(@"^([\d^,-]*)(a*)|/(.*)$");

    static readonly Dictionary<string, string> DefaultReplyForSelectMany = new();
    static readonly Dictionary<string, bool> AlwaysDefaultForSelectMany = new();

    public List<TChoice> SelectMany<TChoice>(List<TChoice> choices,
        Func<TChoice, string> choiceItemString,
        FuncOrValue<List<TChoice>, string>? choiceSummaryString = null, int startingIndex = 1,
        string selectionKey = "", bool skipIfEmpty = true) {
        if (skipIfEmpty && choices.Count == 0) {
            return [];
        }

        AlwaysDefaultForSelectMany.TryAdd(selectionKey, false);
        DefaultReplyForSelectMany.TryAdd(selectionKey, "");

        var chosenIndexes = Enumerable.Range(0, choices.Count).ToList();

        while (true) {
            var selectedChoices = chosenIndexes.Select(index => choices[index]).ToList();
            for (var i = 0; i < selectedChoices.Count; i++) {
                Console.WriteLine($"[{i + startingIndex}]\t{choiceItemString(selectedChoices[i])}");
            }

            var reply = "";
            var flags = "";
            Glob? glob = null;

            if (AlwaysDefaultForSelectMany[selectionKey]) {
                reply = DefaultReplyForSelectMany[selectionKey];
                Console.WriteLine($"Automatically chose [{reply}] as previously instructed.\n");
            } else {
                var messages = new[] {
                    $"Hint: Default for all, prefix '^' for invert, '-' for inclusive range, ',' for combination, eg '{startingIndex}' '-{startingIndex + 3}' '^{startingIndex + 2})'.",
                    "      '?' to restart, '/xxx' for a glob matching with xxx",
                    $"Select 0 or more from the above {selectedChoices.Count} {choiceSummaryString?.Get(selectedChoices) ?? "items"} [{startingIndex}-{startingIndex + selectedChoices.Count - 1}]: "
                };

                Console.Write(messages.JoinBy("\n"));

                var match = ManyChoiceRegex.Match(Console.ReadLine() ?? "");
                while (!match.Success) {
                    Console.WriteLine("Invalid choice. Try again:");
                    Console.WriteLine(messages[0]);
                    Console.Write(messages[1]);
                    match = ManyChoiceRegex.Match(Console.ReadLine() ?? "");
                }

                if (match.Groups[3].Success) {
                    glob = new Glob(match.Groups[3].Value);
                } else {
                    reply = match.Groups[1].Value;
                    flags = match.Groups[2].Value;
                }
            }

            if (glob != null) {
                chosenIndexes = chosenIndexes.Zip(selectedChoices)
                    .Where(item => glob.IsMatch(choiceItemString(item.Second)))
                    .Select(item => item.First).ToList();
                continue;
            }

            if (reply == "") {
                if (flags.Contains('a')) {
                    // Only used when alwaysDefault is true. Otherwise, all is always the default.
                    DefaultReplyForSelectMany[selectionKey] = reply;
                    AlwaysDefaultForSelectMany[selectionKey] = true;
                }

                Logger.Debug(
                    $"Selected {chosenIndexes.Count} {choiceSummaryString?.Get(selectedChoices) ?? "items"} above.");
                return selectedChoices;
            }

            if (reply == "^") {
                chosenIndexes = [];
                return [];
            }

            chosenIndexes = reply.Split(',').Select(selection => {
                var excluded = selection.StartsWith('^');
                selection = selection.TrimStart('^');
                int rangeStart, rangeEnd;
                if (selection.Contains('-')) {
                    var indexes = selection.Split('-');
                    rangeStart = indexes[0].Length > 0 ? int.Parse(indexes[0]) - startingIndex : 0;
                    rangeEnd = indexes[1].Length > 0
                        ? int.Parse(indexes[1]) - startingIndex + 1
                        : chosenIndexes.Count;
                } else {
                    rangeStart = int.Parse(selection) - startingIndex;
                    rangeEnd = rangeStart + 1;
                }

                return (Excluded: excluded, RangeStart: rangeStart, RangeEnd: rangeEnd);
            }).Aggregate<(bool Excluded, int RangeStart, int RangeEnd), IEnumerable<int>?>(null,
                (result, next) => {
                    if (result == null) {
                        return next.Excluded
                            ? Enumerable.Range(0, next.RangeStart).Concat(
                                Enumerable.Range(next.RangeEnd,
                                    chosenIndexes.Count - next.RangeEnd))
                            : Enumerable.Range(next.RangeStart, next.RangeEnd - next.RangeStart);
                    }

                    if (next.Excluded) {
                        return result.Except(Enumerable.Range(next.RangeStart,
                            next.RangeEnd - next.RangeStart));
                    }

                    return result.Union(Enumerable.Range(next.RangeStart,
                        next.RangeEnd - next.RangeStart));
                }).Checked().Select(index => chosenIndexes[index]).ToList();
        }
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

    public bool Confirm(string prefix, bool suggested = true, bool alwaysConfirm = true) {
        if (!alwaysConfirm && AutoConfirmDefault) {
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
