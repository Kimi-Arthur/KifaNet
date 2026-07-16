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

    protected void ExecuteItem(string item, Action action, bool throwIfError = false) {
        Logger.Info($"{item}:");
        Results.Add((item,
            Logger.LogResult(KifaActionResult.FromAction(action), item, LogLevel.Info,
                throwIfError: throwIfError)));
        // To space out between tasks, and also before final result.
        Console.WriteLine();
    }

    protected void ExecuteItem(string item, Func<KifaActionResult> action,
        bool throwIfError = false) {
        Logger.Info($"{item}:");
        Results.Add((item,
            Logger.LogResult(KifaActionResult.FromAction(action), item, LogLevel.Info,
                throwIfError: throwIfError)));
        // To space out between tasks, and also before final result.
        Console.WriteLine();
    }

    public int LogSummary() {
        LogBreakdown();

        var okItems = Results.Where(item => item.result.Status == KifaActionStatus.OK).ToList();
        if (okItems.Count > 0) {
            foreach (var (item, result) in okItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Info($"Successfully processed the {okItems.Count} items above.\n");
        }

        var skippedItems = Results.Where(item => item.result.Status == KifaActionStatus.Skipped).ToList();
        if (skippedItems.Count > 0) {
            foreach (var (item, result) in skippedItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Info($"Skipped the {skippedItems.Count} items above.\n");
        }

        var warningItems = Results.Where(item => item.result.Status == KifaActionStatus.Warning).ToList();
        if (warningItems.Count > 0) {
            foreach (var (item, result) in warningItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Warn($"Processed the {warningItems.Count} items above with warnings.\n");
        }

        var pendingItems = Results.Where(item => item.result.Status == KifaActionStatus.Pending).ToList();
        if (pendingItems.Count > 0) {
            foreach (var (item, result) in pendingItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Error($"The final state of the {pendingItems.Count} items above is pending.\n");
        }

        var badRequestItems = Results.Where(item => item.result.Status == KifaActionStatus.BadRequest).ToList();
        if (badRequestItems.Count > 0) {
            foreach (var (item, result) in badRequestItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Error($"Failed to process the {badRequestItems.Count} items above due to bad requests.\n");
        }

        var errorItems = Results.Where(item => item.result.Status == KifaActionStatus.Error).ToList();
        if (errorItems.Count > 0) {
            foreach (var (item, result) in errorItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Error($"Failed to process the {errorItems.Count} items above due to errors.\n");
        }

        var fileTargets = LogManager.Configuration?.AllTargets.OfType<NLog.Targets.FileTarget>();
        if (fileTargets != null) {
            foreach (var target in fileTargets) {
                var fileName = target.FileName.Render(new LogEventInfo());
                Logger.Info($"Log file: {fileName}");
            }
        }

        LogBreakdown();

        var hasFailed = Results.Any(item => !item.result.IsAcceptable);
        return hasFailed ? 1 : 0;
    }

    void LogBreakdown() {
        var okCount = Results.Count(item => item.result.Status == KifaActionStatus.OK);
        var skippedCount = Results.Count(item => item.result.Status == KifaActionStatus.Skipped);
        var warningCount = Results.Count(item => item.result.Status == KifaActionStatus.Warning);
        var pendingCount = Results.Count(item => item.result.Status == KifaActionStatus.Pending);
        var badRequestCount = Results.Count(item => item.result.Status == KifaActionStatus.BadRequest);
        var errorCount = Results.Count(item => item.result.Status == KifaActionStatus.Error);

        Logger.Info($"Finished processing of {Results.Count} items:");
        if (okCount > 0) {
            Logger.Info($"    OK: {okCount}");
        }
        if (skippedCount > 0) {
            Logger.Info($"    Skipped: {skippedCount}");
        }
        if (warningCount > 0) {
            Logger.Warn($"    Warning: {warningCount}");
        }
        if (pendingCount > 0) {
            Logger.Error($"    Pending: {pendingCount}");
        }
        if (badRequestCount > 0) {
            Logger.Error($"    BadRequest: {badRequestCount}");
        }
        if (errorCount > 0) {
            Logger.Error($"    Error: {errorCount}");
        }
        Console.WriteLine();
    }
}
