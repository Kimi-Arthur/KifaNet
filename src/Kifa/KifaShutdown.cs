using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Kifa;

public static class KifaShutdown {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly ConcurrentBag<Action> Cleanups = [];

    static readonly TimeSpan MaxCleanupWait = TimeSpan.FromSeconds(3);

    static int cleanupRan;

    static KifaShutdown() {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => RunCleanups();
    }

    public static void RegisterCleanup(Action cleanup) {
        Cleanups.Add(cleanup);
    }

    public static void RunCleanups() {
        if (Interlocked.Exchange(ref cleanupRan, 1) != 0) {
            return;
        }

        var tasks = new List<Task>();
        foreach (var cleanup in Cleanups) {
            tasks.Add(Task.Run(() => {
                try {
                    cleanup();
                } catch (Exception ex) {
                    Logger.Warn(ex, "Failed to run shutdown cleanup.");
                }
            }));
        }

        try {
            Task.WaitAll([.. tasks], MaxCleanupWait);
        } catch (Exception ex) {
            Logger.Warn(ex, "Exception waiting for shutdown cleanups.");
        }
    }
}
