using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static bool alwaysDefault;
    static int defaultIndex;

    static readonly Regex ChoiceRegex = new Regex(@"^(\d*)([asi]*)$");

    public static (TChoice Choice, int Index, bool Special)? SelectOne<TChoice>(
        List<TChoice> choices, Func<TChoice, string> choiceToString = null,
        string? choiceName = null, int startingIndex = 0, bool supportsSpecial = false,
        bool reverse = false) {
        var choiceStrings = choiceToString == null
            ? choices.Select(c => c.ToString()).ToList()
            : choices.Select(choiceToString).ToList();

        choiceName ??= "items";

        if (reverse) {
            for (var i = choices.Count - 1; i >= 0; i--) {
                Console.WriteLine($"[{i + startingIndex}] {choiceStrings[i]}");
            }
        } else {
            for (var i = 0; i < choices.Count; i++) {
                Console.WriteLine($"[{i + startingIndex}] {choiceStrings[i]}");
            }
        }

        if (defaultIndex >= choices.Count) {
            defaultIndex = 0;
        }

        Console.WriteLine(
            $"\nDefault [{defaultIndex + startingIndex}]: {choiceStrings[defaultIndex]}\n");

        if (alwaysDefault) {
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
        var match = ChoiceRegex.Match(Console.ReadLine() ?? "");
        while (!match.Success) {
            Console.WriteLine("Invalid choice. Try again:");
            Console.Write($"Default is [{defaultIndex + startingIndex}]: ");
            match = ChoiceRegex.Match(Console.ReadLine() ?? "");
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
            alwaysDefault = true;
        }

        var special = flags.Contains('s');
        if (!supportsSpecial && special) {
            throw new InvalidChoiceException("Special is not supported...");
        }

        defaultIndex = chosenIndex;
        if (chosenIndex < 0 || chosenIndex >= choices.Count) {
            throw new InvalidChoiceException($"Choice {chosenIndex} is out of range.");
        }

        if (alwaysDefault) {
            Console.WriteLine($"Will always choose [{chosenIndex + startingIndex}] from now on.\n");
        }

        return (choices[chosenIndex], chosenIndex, special);
    }

    public static List<TChoice> SelectMany<TChoice>(List<TChoice> choices,
        Func<TChoice, string>? choiceToString = null, string? choiceName = null,
        int startingIndex = 1) {
        choiceToString ??= c => c.ToString();

        choiceName ??= "items";

        while (true) {
            for (var i = 0; i < choices.Count; i++) {
                Console.WriteLine($"[{i + startingIndex}] {choiceToString(choices[i])}");
            }

            Console.WriteLine(
                $"Hint: Default for all, prefix '^' for invert, '-' for inclusive range, ',' for combination, eg '{startingIndex}' '-{startingIndex + 3}' '^{startingIndex + 2})'.");
            Console.Write(
                $"Select 0 or more from above {choices.Count} {choiceName} [{startingIndex}-{startingIndex + choices.Count - 1}]: ");
            var reply = Console.ReadLine() ?? "";
            var chosenIndexes = (reply switch {
                "" => Enumerable.Range(0, choices.Count).ToList(),
                "^" => [],
                _ => reply.Split(',').Select(selection => {
                    var inverted = selection.StartsWith('^');
                    selection = selection.TrimStart('^');
                    int rangeStart, rangeEnd;
                    if (selection.Contains('-')) {
                        var indexes = selection.Split('-');
                        rangeStart = indexes[0].Length > 0
                            ? int.Parse(indexes[0]) - startingIndex
                            : 0;
                        rangeEnd = indexes[1].Length > 0
                            ? int.Parse(indexes[1]) - startingIndex + 1
                            : choices.Count;
                    } else {
                        rangeStart = int.Parse(selection) - startingIndex;
                        rangeEnd = rangeStart + 1;
                    }

                    return (Inverted: inverted, RangeStart: rangeStart, RangeEnd: rangeEnd);
                }).Aggregate<(bool Inverted, int RangeStart, int RangeEnd), IEnumerable<int>?>(null,
                    (result, next) => {
                        if (result == null) {
                            return next.Inverted
                                ? Enumerable.Range(0, next.RangeStart).Concat(
                                    Enumerable.Range(next.RangeEnd, choices.Count - next.RangeEnd))
                                : Enumerable.Range(next.RangeStart,
                                    next.RangeEnd - next.RangeStart);
                        }

                        if (next.Inverted) {
                            return result.Except(Enumerable.Range(next.RangeStart,
                                next.RangeEnd - next.RangeStart));
                        } else {
                            return result.Union(Enumerable.Range(next.RangeStart,
                                next.RangeEnd - next.RangeStart));
                        }
                    }).Checked().ToList()
            });

            if (chosenIndexes.Count == choices.Count) {
                // No need to reconfirm as the selection is for all.
                return choices;
            }

            var chosen = chosenIndexes.Select(index => choices[index]).ToList();
            foreach (var choice in chosen) {
                Console.WriteLine(choiceToString(choice));
            }

            if (!Confirm(
                    $"Confirm selection of {chosen.Count} {choiceName} above out of {choices.Count}?")) {
                continue;
            }

            Logger.Debug($"Selected {chosen.Count} {choiceName} above out of {choices.Count}.");
            return chosen;
        }
    }

    public static string Confirm(string prefix, string suggested) {
        while (true) {
            Console.WriteLine($"{prefix}\n\n{suggested}");

            var line = Console.ReadLine();
            if (line == "") {
                return suggested;
            }

            suggested = line;
        }
    }

    public static bool Confirm(string prefix, bool suggested = true) {
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
