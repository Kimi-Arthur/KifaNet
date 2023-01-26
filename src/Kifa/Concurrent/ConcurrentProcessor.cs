using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Kifa;

public class ConcurrentProcessor<T> {
    ConcurrentQueue<(Func<T> Task, (int RetryCount, DateTimeOffset LastExecution) Status)> Tasks {
        get;
        set;
    }

    ConcurrentQueue<T> Results { get; set; }

    public required Func<T, bool?> Validator { get; init; }

    public int TotalRetryCount { get; init; } = 5;

    public TimeSpan WaitDuration { get; init; } = TimeSpan.FromSeconds(10);

    bool StopWhenFullyProcessed { get; set; }

    public void Add(Func<T> task) {
        Tasks.Enqueue((task, (0, DateTimeOffset.Now)));
    }

    public void Start(int parallelThreads) {
        for (var i = 0; i < parallelThreads; i++) {
            new Thread(() => {
                while (!StopWhenFullyProcessed) {
                    if (Tasks.TryDequeue(out var task)) {
                        SleepIfNeeded(task.Status.LastExecution);
                        var result = task.Task.Invoke();
                        var validation = Validator(result);
                        if (validation == true) {
                            Results.Enqueue(result);
                            continue;
                        }

                        if (validation == false || task.Status.RetryCount >= TotalRetryCount) {
                            Results.Enqueue(result);
                            continue;
                        }

                        Tasks.Enqueue((task.Task,
                            (task.Status.RetryCount + 1, DateTimeOffset.Now)));
                    } else {
                        Thread.Sleep(WaitDuration);
                    }
                }
            }).Start();
        }
    }

    void SleepIfNeeded(DateTimeOffset lastExecution) {
        var target = WaitDuration - (DateTimeOffset.Now - lastExecution);
        if (target > TimeSpan.Zero) {
            Thread.Sleep(target);
        }
    }

    public void Stop() {
        StopWhenFullyProcessed = true;
        while (!Tasks.IsEmpty) {
            Thread.Sleep(WaitDuration);
        }
    }
}
