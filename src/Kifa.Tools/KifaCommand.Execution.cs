using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    List<(string item, KifaActionResult result)> Results { get; set; } = new();

    protected void ExecuteItem(string item, Action action)
        => Results.Add((item,
            Logger.LogResult(KifaActionResult.FromAction(action), $"action on {item}")));

    protected void ExecuteItem(string item, Func<KifaActionResult> action)
        => Results.Add((item,
            Logger.LogResult(KifaActionResult.FromAction(action), $"action on {item}")));

    public int LogSummary() {
        var resultsByStatus = Results.GroupBy(item => item.result.IsAcceptable)
            .ToDictionary(item => item.Key, item => item.ToList());
        if (resultsByStatus.ContainsKey(true)) {
            var items = resultsByStatus[true];
            Logger.Info($"Successfully acted on the following {items.Count} items:");
            foreach (var (item, result) in items) {
                Logger.Info($"{item}:");
                foreach (var line in (result.Message ?? "OK").Split("\n")) {
                    Logger.Info($"\t{line}");
                }
            }
        }

        if (resultsByStatus.ContainsKey(false)) {
            var items = resultsByStatus[false];
            Logger.Error($"Failed to act on the following {items.Count} items:");
            foreach (var (item, result) in items) {
                Logger.Error($"{item} =>");
                foreach (var line in (result.Message ?? "Unknown error").Split("\n")) {
                    Logger.Error($"\t{line}");
                }
            }

            return 1;
        }

        return 0;
    }
}
