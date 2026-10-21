using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Service;
using Kifa.YouTube;

namespace Kifa.Web.Api.Controllers;

public partial class YouTubeAccountJsonServiceClient {
    class SessionWaiter {
        public required string AccountId { get; init; }
        public string? SessionId { get; init; }
        public required YouTubeSessionType Type { get; init; }
        public required TaskCompletionSource<KifaActionResult<YouTubeSession>> TaskCompletionSource { get; init; }
        public string? DataFolder { get; init; }
    }

    static DateTimeOffset? GetNextExpiration(YouTubeAccount account, YouTubeSessionType type,
        string? sessionId) {
        var now = DateTimeOffset.UtcNow;
        var expirations = new List<DateTimeOffset>();

        if (sessionId != null) {
            var fullSessionId = NormalizeSessionId(account.Id.Checked(), sessionId);
            var reservation = account.Sessions.FirstOrDefault(s => s.Session.Id == fullSessionId);
            if (reservation != null && reservation.Reserved > now) {
                expirations.Add(reservation.Reserved);
            }
        }

        var limit = YouTubeAccount.GetSessionLimit(type);
        var activeTypeSessions = account.Sessions.Where(s => s.Reserved > now && s.Type == type)
            .ToList();
        if (activeTypeSessions.Count >= limit && activeTypeSessions.Count > 0) {
            expirations.Add(activeTypeSessions.Min(s => s.Reserved));
        }

        var activeSessions = account.Sessions.Where(s => s.Reserved > now).ToList();
        if (activeSessions.Count >= account.Sessions.Count && activeSessions.Count > 0) {
            expirations.Add(activeSessions.Min(s => s.Reserved));
        }

        return expirations.Count > 0 ? expirations.Min() : null;
    }

    static readonly ConcurrentDictionary<string, Timer> ExpirationTimers = new();
    static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMilliseconds(100);

    static void ScheduleExpirationCheck(string accountId, DateTimeOffset targetTime) {
        var delay = targetTime - DateTimeOffset.UtcNow + ExpirationBuffer;
        if (delay < TimeSpan.Zero) {
            delay = TimeSpan.Zero;
        }

        ExpirationTimers.AddOrUpdate(accountId,
            id => new Timer(OnLeaseExpired, id, delay, Timeout.InfiniteTimeSpan),
            (id, existingTimer) => {
                try {
                    existingTimer.Change(delay, Timeout.InfiniteTimeSpan);
                    return existingTimer;
                } catch (ObjectDisposedException) {
                    return new Timer(OnLeaseExpired, id, delay, Timeout.InfiniteTimeSpan);
                }
            });
    }

    static void CancelExpirationCheck(string accountId) {
        if (ExpirationTimers.TryRemove(accountId, out var timer)) {
            timer.Dispose();
        }
    }

    static readonly List<SessionWaiter> Waiters = new();
    static readonly object WaitersLock = new();

    static void OnLeaseExpired(object? state) {
        var accountId = state as string;
        if (accountId == null) {
            return;
        }

        string? dataFolder;
        lock (WaitersLock) {
            dataFolder = Waiters.FirstOrDefault(w => w.AccountId == accountId)?.DataFolder;
        }

        try {
            var client = new YouTubeAccountJsonServiceClient();
            if (dataFolder != null) {
                client.DataFolder = dataFolder;
            }

            client.NotifyWaiters(accountId);
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to process expired lease check for account {accountId}.");
        }
    }

    public void NotifyWaiters(string accountId) {
        using (AcquireLock(accountId)) {
            List<SessionWaiter> candidates;
            lock (WaitersLock) {
                candidates = Waiters.Where(w => w.AccountId == accountId).ToList();
            }

            foreach (var waiter in candidates) {
                lock (WaitersLock) {
                    if (!Waiters.Contains(waiter)) {
                        continue;
                    }
                }

                if (waiter.TaskCompletionSource.Task.IsCompleted) {
                    lock (WaitersLock) {
                        Waiters.Remove(waiter);
                    }

                    continue;
                }

                var result = ObtainSession(waiter.AccountId, waiter.Type, waiter.SessionId);
                if (result.Status == KifaActionStatus.Pending) {
                    continue;
                }

                lock (WaitersLock) {
                    Waiters.Remove(waiter);
                }

                if (result.Status == KifaActionStatus.OK) {
                    Logger.Debug(
                        $"Granted {waiter.Type} session {result.Value?.Id} to waiter for account {waiter.AccountId}.");
                } else {
                    Logger.Debug(
                        $"Failed to obtain {waiter.Type} session for waiter for account {waiter.AccountId}: {result.Status} - {result.Message}");
                }

                if (!waiter.TaskCompletionSource.TrySetResult(result)) {
                    if (result.Status == KifaActionStatus.OK && result.Value?.Id != null) {
                        ReleaseSession(waiter.AccountId, result.Value.Id);
                    }
                }
            }

            List<SessionWaiter> remainingWaiters;
            lock (WaitersLock) {
                remainingWaiters = Waiters.Where(w => w.AccountId == accountId).ToList();
            }

            if (remainingWaiters.Count == 0) {
                CancelExpirationCheck(accountId);
            } else {
                var account = Get(accountId);
                if (account != null) {
                    var expirations = remainingWaiters
                        .Select(w => GetNextExpiration(account, w.Type, w.SessionId))
                        .ExceptNull()
                        .ToList();

                    if (expirations.Count > 0) {
                        ScheduleExpirationCheck(accountId, expirations.Min());
                    } else {
                        CancelExpirationCheck(accountId);
                    }
                }
            }
        }
    }

    public TimeSpan WaitTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public async Task<KifaActionResult<YouTubeSession>> ObtainSessionAsync(string accountId,
        YouTubeSessionType type, string? sessionId = null,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        NotifyWaiters(accountId);

        var initialResult = ObtainSession(accountId, type, sessionId);
        if (initialResult.Status != KifaActionStatus.Pending) {
            return initialResult;
        }

        var timeout = WaitTimeout;
        if (timeout <= TimeSpan.Zero) {
            return initialResult;
        }

        Logger.Debug(
            $"No available {type} session for account {accountId} ({initialResult.Message}). Waiting for available session...");

        var waiter = new SessionWaiter {
            AccountId = accountId,
            SessionId = sessionId,
            Type = type,
            DataFolder = DataFolder,
            TaskCompletionSource = new TaskCompletionSource<KifaActionResult<YouTubeSession>>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };

        lock (WaitersLock) {
            Waiters.Add(waiter);
        }

        NotifyWaiters(accountId);

        if (waiter.TaskCompletionSource.Task.IsCompleted) {
            return await waiter.TaskCompletionSource.Task;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try {
            return await waiter.TaskCompletionSource.Task.WaitAsync(cts.Token);
        } catch (OperationCanceledException) {
            lock (WaitersLock) {
                Waiters.Remove(waiter);
            }

            if (waiter.TaskCompletionSource.Task.IsCompletedSuccessfully) {
                return waiter.TaskCompletionSource.Task.Result;
            }

            waiter.TaskCompletionSource.TrySetCanceled(cts.Token);

            if (cancellationToken.IsCancellationRequested) {
                throw;
            }

            Logger.Warn($"Timed out waiting for {type} session for account {accountId}.");

            return new KifaActionResult<YouTubeSession> {
                Status = KifaActionStatus.Pending,
                Message =
                    $"Timeout waiting for {type} session for account {accountId}: {initialResult.Message}"
            };
        } finally {
            lock (WaitersLock) {
                Waiters.Remove(waiter);
                if (Waiters.All(w => w.AccountId != accountId)) {
                    CancelExpirationCheck(accountId);
                }
            }
        }
    }
}
