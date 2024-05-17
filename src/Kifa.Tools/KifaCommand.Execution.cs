using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using NLog;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    List<(string item, KifaActionResult result)> Results { get; set; } = new();

    protected List<(string item, KifaActionResult result)> PopPendingResults() {
        var pendingResults = Results.Where(r => r.result.Status.HasFlag(KifaActionStatus.Pending))
            .ToList();
        foreach (var r in pendingResults) {
            Results.Remove(r);
        }

        return pendingResults;
    }

    protected void ExecuteItem(string item, Action action) {
        Logger.Info($"Action on {item} started.");
        Results.Add((item,
            Logger.LogResult(KifaActionResult.FromAction(action), $"action on {item}", LogLevel.Info)));
    }

    protected void ExecuteItem(string item, Func<KifaActionResult> action) {
        Logger.Info($"Action on {item} started.");
        Results.Add((item,
            Logger.LogResult(KifaActionResult.FromAction(action), $"action on {item}", LogLevel.Info)));
    }

    public int LogSummary() {
        var resultsByStatus = Results.GroupBy(item => item.result.IsAcceptable)
            .ToDictionary(item => item.Key, item => item.ToList());
        if (resultsByStatus.TryGetValue(true, out var acceptableItems)) {
            Logger.Info($"Successfully acted on the following {acceptableItems.Count} items:");
            foreach (var (item, result) in acceptableItems) {
                var level = result.Status == KifaActionStatus.Warning
                    ? LogLevel.Warn
                    : LogLevel.Info;
                Logger.Log(level, $"{item}:");
                foreach (var line in (result.Message ?? "OK").Split("\n")) {
                    Logger.Log(level, $"\t{line}");
                }
            }
        }

        if (resultsByStatus.TryGetValue(false, out var failedItems)) {
            Logger.Error($"Failed to act on the following {failedItems.Count} items:");
            foreach (var (item, result) in failedItems) {
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
