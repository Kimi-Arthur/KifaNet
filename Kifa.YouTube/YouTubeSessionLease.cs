using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kifa.Service;
using NLog;

namespace Kifa.YouTube;

public class YouTubeSessionLease : IDisposable {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static TimeSpan QuickKeepSessionInterval { get; set; } = TimeSpan.FromMinutes(1);
    public static TimeSpan LongKeepSessionInterval { get; set; } = TimeSpan.FromMinutes(5);

    public string AccountId { get; }
    public YouTubeSession Session { get; }
    public YouTubeSessionType Type { get; }
    public string? CookieFile { get; }

    readonly CancellationTokenSource cancellationTokenSource = new();
    bool disposed;

    public YouTubeSessionLease(string accountId, YouTubeSession session, YouTubeSessionType type) {
        AccountId = accountId;
        Session = session;
        Type = type;

        if (session.Cookies != null) {
            CookieFile = Path.GetTempFileName();
            File.WriteAllText(CookieFile, session.Cookies);
        }

        _ = KeepSessionReserved();
    }

    async Task KeepSessionReserved() {
        var interval = Type switch {
            YouTubeSessionType.Quick => QuickKeepSessionInterval,
            YouTubeSessionType.Download => LongKeepSessionInterval,
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null)
        };

        while (!cancellationTokenSource.IsCancellationRequested) {
            try {
                await Task.Delay(interval, cancellationTokenSource.Token);
            } catch (OperationCanceledException) {
                break;
            }

            if (disposed) {
                break;
            }

            Logger.LogResult(YouTubeAccount.Client.RenewSession(AccountId, Session.Id),
                $"reserving session {Session.Id}", defaultLevel: LogLevel.Trace);
        }
    }

    public void Dispose() {
        if (disposed) {
            return;
        }

        disposed = true;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();

        try {
            string? updatedCookies = null;
            if (CookieFile != null && File.Exists(CookieFile)) {
                updatedCookies = File.ReadAllText(CookieFile);
            }

            YouTubeAccount.Client.ReleaseSession(AccountId, Session.Id, updatedCookies);
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to release session {Session.Id} for account {AccountId}.");
        } finally {
            if (CookieFile != null && File.Exists(CookieFile)) {
                try {
                    File.Delete(CookieFile);
                } catch {
                }
            }
        }
    }
}
