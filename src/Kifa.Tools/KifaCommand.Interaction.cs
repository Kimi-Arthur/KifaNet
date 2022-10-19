using System;
using System.Collections.Generic;
using System.Linq;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static bool alwaysDefault;
    static int defaultIndex = 1;

    public static (TChoice Choice, int Index, bool Special) SelectOne<TChoice>(
        List<TChoice> choices, Func<TChoice, string> choiceToString = null,
        string choiceName = null) {
        var choiceStrings = choiceToString == null
            ? choices.Select(c => c.ToString()).ToList()
            : choices.Select(choiceToString).ToList();

        choiceName ??= "items";

        for (var i = 0; i < choices.Count; i++) {
            Console.WriteLine($"[{i + 1}] {choiceStrings[i]}");
        }

        Console.WriteLine($"\nDefault [{defaultIndex}]: {choiceStrings[defaultIndex - 1]}\n");

        if (alwaysDefault) {
            Console.WriteLine($"Automatically chose [{defaultIndex}] as previously instructed.\n");
            return (choices[defaultIndex - 1], defaultIndex - 1, false);
        }

        Console.WriteLine(
            $"Choose one from the {choiceName} above [1-{choices.Count}]. Append 'a' to always choose the same index,");
        Console.Write($"Default is [{defaultIndex}]: ");
        var line = Console.ReadLine();

        if (line.EndsWith('a')) {
            alwaysDefault = true;
            line = line[..^1];
        }

        var special = line.EndsWith('s');

        var chosenIndex = string.IsNullOrEmpty(line) ? defaultIndex : int.Parse(line);
        defaultIndex = chosenIndex;
        if (chosenIndex < 1 || chosenIndex > choices.Count) {
            throw new InvalidChoiceException($"Choice {chosenIndex} is out of range.");
        }

        if (alwaysDefault) {
            Console.WriteLine($"Will always choose [{chosenIndex}] from now on.\n");
        }

        return (choices[chosenIndex - 1], chosenIndex - 1, special);
    }

    public static List<TChoice> SelectMany<TChoice>(List<TChoice> choices,
        Func<TChoice, string> choiceToString = null, string choiceName = null) {
        var choiceStrings = choiceToString == null
            ? choices.Select(c => c.ToString()).ToList()
            : choices.Select(choiceToString).ToList();

        choiceName ??= "items";

        for (var i = 0; i < choices.Count; i++) {
            Console.WriteLine($"[{i}] {choiceStrings[i]}");
        }

        Console.Write(
            $"Choose 0 or more from above {choices.Count} {choiceName} [0-{choices.Count - 1}] (default is all, . is nothing): ");
        var reply = Console.ReadLine() ?? "";
        var chosen = reply == "" ? choices :
            reply == "." ? new List<TChoice>() : reply.Split(',').SelectMany(i => i.Contains('-')
                ? choices.Take(int.Parse(i.Substring(i.IndexOf('-') + 1)) + 1)
                    .Skip(int.Parse(i.Substring(0, i.IndexOf('-'))))
                : new List<TChoice> {
                    choices[int.Parse(i)]
                }).ToList();
        Logger.Debug($"Selected {chosen.Count} out of {choices.Count} {choiceName}.");
        return chosen;
    }

    public static string Confirm(string prefix, string suggested) {
        while (true) {
            Console.WriteLine($"{prefix} [{suggested}]?");

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
