using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using NLog;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    public List<(string item, KifaActionResult result)> Results { get; set; } = new();

    protected List<(string item, KifaActionResult result)> PopPendingResults() {
        lock (Results) {
            var pendingResults = Results.Where(r => r.result.Status.HasFlag(KifaActionStatus.Pending))
                .ToList();
            foreach (var r in pendingResults) {
                Results.Remove(r);
            }

            return pendingResults;
        }
    }

    protected void ExecuteItem(string item, Action action, bool throwIfError = false,
        bool silent = false) {
        WaitIfPaused(item);

        if (StopRequested) {
            lock (Results) {
                Results.Add((item, new KifaActionResult {
                    Status = KifaActionStatus.Cancelled,
                    Message = "Cancelled as stop was requested by user."
                }));
            }
            return;
        }

        CurrentItem = item;
        try {
            if (silent) {
                var result = KifaActionResult.FromAction(action);
                lock (Results) {
                    Results.Add((item, result));
                }

                if (throwIfError && (result.Status.HasFlag(KifaActionStatus.Error) ||
                                     result.Status.HasFlag(KifaActionStatus.BadRequest))) {
                    throw new KifaActionFailedException(result);
                }
            } else {
                Logger.Info($"{item}:");
                var result = Logger.LogResult(KifaActionResult.FromAction(action), item, LogLevel.Info,
                    throwIfError: throwIfError);
                lock (Results) {
                    Results.Add((item, result));
                }

                // To space out between tasks, and also before final result.
                Console.WriteLine();
            }
        } finally {
            CurrentItem = null;
        }
    }

    protected void ExecuteItem(string item, Func<KifaActionResult> action,
        bool throwIfError = false, bool silent = false) {
        WaitIfPaused(item);

        if (StopRequested) {
            lock (Results) {
                Results.Add((item, new KifaActionResult {
                    Status = KifaActionStatus.Cancelled,
                    Message = "Cancelled as stop was requested by user."
                }));
            }
            return;
        }

        CurrentItem = item;
        try {
            if (silent) {
                var result = KifaActionResult.FromAction(action);
                lock (Results) {
                    Results.Add((item, result));
                }

                if (throwIfError && (result.Status.HasFlag(KifaActionStatus.Error) ||
                                     result.Status.HasFlag(KifaActionStatus.BadRequest))) {
                    throw new KifaActionFailedException(result);
                }
            } else {
                Logger.Info($"{item}:");
                var result = Logger.LogResult(KifaActionResult.FromAction(action), item, LogLevel.Info,
                    throwIfError: throwIfError);
                lock (Results) {
                    Results.Add((item, result));
                }

                // To space out between tasks, and also before final result.
                Console.WriteLine();
            }
        } finally {
            CurrentItem = null;
        }
    }

    public int LogSummary() {
        List<(string item, KifaActionResult result)> results;
        lock (Results) {
            results = Results.ToList();
        }

        LogBreakdown(results);

        var okItems = results.Where(item => item.result.Status == KifaActionStatus.OK).ToList();
        if (okItems.Count > 0) {
            if (!NonVerbose) {
                foreach (var (item, result) in okItems) {
                    Logger.LogResult(result, item, LogLevel.Info);
                }

                Logger.Info($"Successfully processed the {okItems.Count} items above.\n");
            } else {
                Logger.Info($"Successfully processed {okItems.Count} items.\n");
            }
        }

        var skippedItems = results.Where(item => item.result.Status == KifaActionStatus.Skipped).ToList();
        if (skippedItems.Count > 0) {
            if (!NonVerbose) {
                foreach (var (item, result) in skippedItems) {
                    Logger.LogResult(result, item, LogLevel.Info);
                }

                Logger.Info($"Skipped the {skippedItems.Count} items above.\n");
            } else {
                Logger.Info($"Skipped {skippedItems.Count} items.\n");
            }
        }

        var cancelledItems = results.Where(item => item.result.Status == KifaActionStatus.Cancelled).ToList();
        if (cancelledItems.Count > 0) {
            if (!NonVerbose) {
                foreach (var (item, result) in cancelledItems) {
                    Logger.LogResult(result, item, LogLevel.Info);
                }

                Logger.Warn($"Cancelled the {cancelledItems.Count} items above.\n");
            } else {
                Logger.Warn($"Cancelled {cancelledItems.Count} items.\n");
            }
        }

        var warningItems = results.Where(item => item.result.Status == KifaActionStatus.Warning).ToList();
        if (warningItems.Count > 0) {
            foreach (var (item, result) in warningItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Warn($"Processed the {warningItems.Count} items above with warnings.\n");
        }

        var pendingItems = results.Where(item => item.result.Status == KifaActionStatus.Pending).ToList();
        if (pendingItems.Count > 0) {
            foreach (var (item, result) in pendingItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Error($"The final state of the {pendingItems.Count} items above is pending.\n");
        }

        var badRequestItems = results.Where(item => item.result.Status == KifaActionStatus.BadRequest).ToList();
        if (badRequestItems.Count > 0) {
            foreach (var (item, result) in badRequestItems) {
                Logger.LogResult(result, item, LogLevel.Info);
            }

            Logger.Error($"Failed to process the {badRequestItems.Count} items above due to bad requests.\n");
        }

        var errorItems = results.Where(item => item.result.Status == KifaActionStatus.Error).ToList();
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

        LogBreakdown(results);

        var hasFailed = results.Any(item => !item.result.IsAcceptable);
        return hasFailed ? 1 : 0;
    }

    void LogBreakdown(List<(string item, KifaActionResult result)> results) {
        var okCount = results.Count(item => item.result.Status == KifaActionStatus.OK);
        var skippedCount = results.Count(item => item.result.Status == KifaActionStatus.Skipped);
        var cancelledCount = results.Count(item => item.result.Status == KifaActionStatus.Cancelled);
        var warningCount = results.Count(item => item.result.Status == KifaActionStatus.Warning);
        var pendingCount = results.Count(item => item.result.Status == KifaActionStatus.Pending);
        var badRequestCount = results.Count(item => item.result.Status == KifaActionStatus.BadRequest);
        var errorCount = results.Count(item => item.result.Status == KifaActionStatus.Error);

        Logger.Info($"Finished processing of {results.Count} items:");
        if (okCount > 0) {
            Logger.Info($"    OK: {okCount}");
        }
        if (skippedCount > 0) {
            Logger.Info($"    Skipped: {skippedCount}");
        }
        if (cancelledCount > 0) {
            Logger.Warn($"    Cancelled: {cancelledCount}");
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
