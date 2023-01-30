using System;
using System.Collections.Concurrent;
using System.Threading;
using NLog;

namespace Kifa;

public class ConcurrentProcessor<T> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    ConcurrentQueue<(Func<T> Task, (int RetryCount, DateTimeOffset LastExecution) Status)> Tasks {
        get;
        set;
    } = new();

    ConcurrentQueue<T> Results { get; set; } = new();

    int RunnerCount { get; set; }

    public required Func<T, bool?> Validator { get; init; }

    public required int TotalRetryCount { get; init; }

    public required TimeSpan CooldownDuration { get; init; }

    static readonly TimeSpan MinIdleDuration = TimeSpan.FromSeconds(10);

    public TimeSpan IdleDuration => Kifa.Max(CooldownDuration, MinIdleDuration);

    bool StopWhenFullyProcessed { get; set; }

    public void Add(Func<T> task) {
        Tasks.Enqueue((task, (0, DateTimeOffset.Now)));
    }

    public void Start(int parallelThreads) {
        RunnerCount = parallelThreads;
        Logger.Debug($"Start {RunnerCount} runners to process tasks.");
        for (var i = 0; i < parallelThreads; i++) {
            new Thread(() => {
                while (!StopWhenFullyProcessed || !Tasks.IsEmpty) {
                    if (Tasks.TryDequeue(out var task)) {
                        Logger.Debug("Processing one task...");
                        SleepIfNeeded(task.Status.LastExecution);
                        var result = task.Task.Invoke();
                        var validation = Validator(result);
                        if (validation == true) {
                            Logger.Debug($"Finished one task successfully. Result: {result}.");
                            Results.Enqueue(result);
                            continue;
                        }

                        if (validation == false || task.Status.RetryCount >= TotalRetryCount) {
                            Logger.Debug($"Failed one task. Result: {result}.");
                            Results.Enqueue(result);
                            continue;
                        }

                        Tasks.Enqueue((task.Task,
                            (task.Status.RetryCount + 1, DateTimeOffset.Now)));
                    } else {
                        Logger.Trace($"No more tasks to process. Sleep {IdleDuration}.");
                        Thread.Sleep(IdleDuration);
                    }
                }

                RunnerCount--;
            }).Start();
        }
    }

    void SleepIfNeeded(DateTimeOffset lastExecution) {
        var target = CooldownDuration - (DateTimeOffset.Now - lastExecution);
        if (target > TimeSpan.Zero) {
            Logger.Trace($"Task needs to cool down for {target}.");
            Thread.Sleep(target);
        }
    }

    public void Stop() {
        StopWhenFullyProcessed = true;
        while (RunnerCount > 0) {
            Logger.Trace($"Waiting for {RunnerCount} runners to finish. Sleep {IdleDuration}.");
            Thread.Sleep(IdleDuration);
        }

        Logger.Debug("All runners finished.");
    }
}
