using ConcurrentPriorityQueue;
using ConcurrentPriorityQueue.Core;

namespace Kifa.Threading;

// A lock that only acquirable only when higher ones are all released and not pending.
public class PriorityLock {
    public class Scope : IDisposable {
        public required PriorityLock ParentLock { get; set; }
        public required TaskCompletionSource<Scope> TaskCompletionSource { get; set; }

        public void Dispose() {
            ParentLock.StartNew();
        }
    }

    public class QueueItem : IHavePriority<int> {
        public int Priority { get; init; }
        public Scope Scope { get; set; }
    }

    readonly ConcurrentPriorityByIntegerQueue<QueueItem> tasks = new();

    // semaphore only to guard entering a new start job loop. It's not constantly acquired and
    // released.
    readonly SemaphoreSlim semaphore = new(1);

    // The lower, the earlier.
    public Task<Scope> EnterScopeAsync(int priority) {
        var tcs = new TaskCompletionSource<Scope>();
        tasks.Enqueue(new QueueItem {
            Scope = new Scope {
                ParentLock = this,
                TaskCompletionSource = tcs
            },
            Priority = priority,
        });

        if (semaphore.Wait(TimeSpan.Zero)) {
            StartNew();
        }

        return tcs.Task;
    }

    internal void StartNew() {
        while (tasks.Count > 0) {
            var item = tasks.Dequeue().Value;
            var tcs = item.Scope.TaskCompletionSource;
            if (tcs.Task.IsCompleted) {
                continue;
            }

            tcs.SetResult(item.Scope);
            return;
        }

        semaphore.Release();
    }
}
