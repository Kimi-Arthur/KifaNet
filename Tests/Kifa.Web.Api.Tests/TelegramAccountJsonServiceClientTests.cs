using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kifa.Cloud.Telegram;
using Kifa.Service;
using Kifa.Web.Api.Controllers;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class TelegramAccountJsonServiceClientTests : IDisposable {
    readonly string folder;
    readonly TelegramAccountJsonServiceClient client = new();

    public TelegramAccountJsonServiceClientTests() {
        folder = $"{Path.GetTempPath()}/{nameof(TelegramAccountJsonServiceClientTests)}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next()}";
        client.DataFolder = folder;
        TelegramAccount.MaxQuickSessions = 5;
        TelegramAccount.MaxUploadSessions = 2;
        TelegramAccount.MaxDownloadSessions = 2;
    }

    public void Dispose() {
        TelegramAccount.MaxQuickSessions = 5;
        TelegramAccount.MaxUploadSessions = 2;
        TelegramAccount.MaxDownloadSessions = 2;
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }

    TelegramAccount CreateAccountWithSessions(string id, int sessionCount) {
        var account = new TelegramAccount {
            Id = id,
            Phone = "+1234567890",
            ApiId = 12345,
            ApiHash = "fake_hash",
            Sessions = new List<TelegramSession>()
        };

        for (var i = 0; i < sessionCount; i++) {
            account.Sessions.Add(new TelegramSession {
                Id = i + 1,
                Data = [1, 2, 3, 4],
                Refreshed = DateTimeOffset.UtcNow,
                Reserved = Date.Zero
            });
        }

        client.Set(account);
        return account;
    }

    [Fact]
    public void AppConfig_DefaultSessionLimits() {
        TelegramAccount.MaxQuickSessions.Should().Be(5);
        TelegramAccount.MaxUploadSessions.Should().Be(2);
        TelegramAccount.MaxDownloadSessions.Should().Be(2);
    }

    [Fact]
    public void AccountSerialization_DoesNotStoreOrReadLimits() {
        var account = new TelegramAccount {
            Id = "no_limits_stored",
            Phone = "+1234567890",
            ApiId = 12345,
            ApiHash = "fake_hash"
        };

        var json = account.ToJson();
        json.Should().NotContain("max_quick_sessions");
        json.Should().NotContain("max_upload_sessions");
        json.Should().NotContain("max_download_sessions");
    }

    [Fact]
    public void ObtainSession_AfterReload_ChecksTypeMarkedOnEachSessionExcludingExpired() {
        var now = DateTimeOffset.UtcNow;
        var rawJson = $$"""
        {
          "id": "reload_test",
          "phone": "+1234567890",
          "api_id": 12345,
          "api_hash": "fake_hash",
          "sessions": [
            {
              "id": 1,
              "data": [1, 2, 3, 4],
              "refreshed": "{{now:yyyy-MM-dd HH:mm:ss.ffffff}}",
              "reserved": "{{now.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}}",
              "type": "Upload"
            },
            {
              "id": 2,
              "data": [1, 2, 3, 4],
              "refreshed": "{{now:yyyy-MM-dd HH:mm:ss.ffffff}}",
              "reserved": "{{now.AddMinutes(-5):yyyy-MM-dd HH:mm:ss.ffffff}}",
              "type": "Upload"
            },
            {
              "id": 3,
              "data": [1, 2, 3, 4],
              "refreshed": "{{now:yyyy-MM-dd HH:mm:ss.ffffff}}",
              "reserved": "{{now.AddMinutes(-10):yyyy-MM-dd HH:mm:ss.ffffff}}",
              "type": "Quick"
            }
          ]
        }
        """;
        Directory.CreateDirectory($"{folder}/telegram/accounts");
        File.WriteAllText($"{folder}/telegram/accounts/reload_test.json", rawJson);

        // Upload limit is 2. Session 1 is active Upload (Reserved > now).
        // Session 2 is expired Upload (Reserved <= now). Session 3 is expired Quick.
        // Active Upload count is 1. We can obtain a second Upload session.
        var secondUpload = client.ObtainSession("reload_test", TelegramSessionType.Upload);
        secondUpload.Status.Should().Be(KifaActionStatus.OK);
        secondUpload.Value.Checked().Type.Should().Be(TelegramSessionType.Upload);

        // Now active Upload count is 2 (Session 1 + newly leased session).
        // A third Upload request should be Pending because limit (2) is reached.
        var thirdUpload = client.ObtainSession("reload_test", TelegramSessionType.Upload);
        thirdUpload.Status.Should().Be(KifaActionStatus.Pending);
        thirdUpload.Message.Should().Be("All 2 Upload sessions are reserved.");
    }

    [Fact]
    public void ObtainSession_WhenLimitsMissingFromJson_CanObtainUpToDefaultLimit() {
        var now = DateTimeOffset.UtcNow;
        var rawJson = $$"""
        {
          "id": "missing_limits_file",
          "phone": "+1234567890",
          "api_id": 12345,
          "api_hash": "fake_hash",
          "sessions": [
            {
              "id": 1,
              "data": [1, 2, 3, 4],
              "refreshed": "{{now:yyyy-MM-dd HH:mm:ss.ffffff}}",
              "reserved": "0001-01-01 00:00:00.000000"
            }
          ]
        }
        """;
        // Write raw JSON directly to disk, simulating legacy storage file like Kimily.json on server
        Directory.CreateDirectory($"{folder}/telegram/accounts");
        File.WriteAllText($"{folder}/telegram/accounts/missing_limits_file.json", rawJson);

        var result = client.ObtainSession("missing_limits_file", TelegramSessionType.Quick);
        result.Status.Should().Be(KifaActionStatus.OK);
        result.Value.Should().NotBeNull();
        result.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
    }

    [Fact]
    public void ObtainSession_QuickLimit_EnforcesLimitAndPending() {
        TelegramAccount.MaxQuickSessions = 1;
        var account = CreateAccountWithSessions("quick_test", 3);

        var first = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        first.Status.Should().Be(KifaActionStatus.OK);
        first.Value.Should().NotBeNull();
        first.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
        first.Value.Checked().Reserved.Should().BeCloseTo(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(5));

        var second = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        second.Status.Should().Be(KifaActionStatus.Pending);
        second.Message.Should().Be("All 1 Quick sessions are reserved.");
    }

    [Fact]
    public void ObtainSession_UploadLimit_EnforcesLimitAndPending() {
        var account = CreateAccountWithSessions("upload_test", 4);

        var first = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        first.Status.Should().Be(KifaActionStatus.OK);
        first.Value.Checked().Type.Should().Be(TelegramSessionType.Upload);
        first.Value.Checked().Reserved.Should().BeCloseTo(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10),
            TimeSpan.FromSeconds(5));

        var second = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        second.Status.Should().Be(KifaActionStatus.OK);
        second.Value.Checked().Type.Should().Be(TelegramSessionType.Upload);

        var third = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        third.Status.Should().Be(KifaActionStatus.Pending);
        third.Message.Should().Be("All 2 Upload sessions are reserved.");
    }

    [Fact]
    public void ObtainSession_DownloadLimit_EnforcesLimitAndPending() {
        var account = CreateAccountWithSessions("download_test", 4);

        var first = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Download);
        first.Status.Should().Be(KifaActionStatus.OK);
        first.Value.Checked().Type.Should().Be(TelegramSessionType.Download);
        first.Value.Checked().Reserved.Should().BeCloseTo(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10),
            TimeSpan.FromSeconds(5));

        var second = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Download);
        second.Status.Should().Be(KifaActionStatus.OK);
        second.Value.Checked().Type.Should().Be(TelegramSessionType.Download);

        var third = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Download);
        third.Status.Should().Be(KifaActionStatus.Pending);
        third.Message.Should().Be("All 2 Download sessions are reserved.");
    }

    [Fact]
    public void ReleaseSession_FreesSlotForReuse() {
        var account = CreateAccountWithSessions("release_test", 1);

        var first = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        first.Status.Should().Be(KifaActionStatus.OK);
        var sessionId = first.Value.Checked().Id;

        var second = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        second.Status.Should().Be(KifaActionStatus.Pending);

        var releaseResult = client.ReleaseSession(account.Id.Checked(), sessionId);
        releaseResult.Status.Should().Be(KifaActionStatus.OK);

        var third = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        third.Status.Should().Be(KifaActionStatus.OK);
        third.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
    }

    [Fact]
    public void ObtainSession_DifferentTypes_CanRunConcurrently() {
        TelegramAccount.MaxQuickSessions = 1;
        var account = CreateAccountWithSessions("concurrent_types", 5);

        var quick = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        quick.Status.Should().Be(KifaActionStatus.OK);

        var upload1 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        upload1.Status.Should().Be(KifaActionStatus.OK);

        var upload2 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        upload2.Status.Should().Be(KifaActionStatus.OK);

        var download1 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Download);
        download1.Status.Should().Be(KifaActionStatus.OK);

        var download2 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Download);
        download2.Status.Should().Be(KifaActionStatus.OK);

        client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick).Status
            .Should().Be(KifaActionStatus.Pending);
        client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload).Status
            .Should().Be(KifaActionStatus.Pending);
        client.ObtainSession(account.Id.Checked(), TelegramSessionType.Download).Status
            .Should().Be(KifaActionStatus.Pending);
    }

    [Fact]
    public void ObtainSession_AllSessionsReserved_ReturnsPending() {
        TelegramAccount.MaxUploadSessions = 5;
        var account = CreateAccountWithSessions("all_reserved", 2);

        var upload1 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        upload1.Status.Should().Be(KifaActionStatus.OK);

        var upload2 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        upload2.Status.Should().Be(KifaActionStatus.OK);

        var upload3 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        upload3.Status.Should().Be(KifaActionStatus.Pending);
        upload3.Message.Should().Be("All 2 sessions are reserved.");
    }

    [Fact]
    public void ObtainSession_CustomLimits_Respected() {
        TelegramAccount.MaxQuickSessions = 2;
        var account = CreateAccountWithSessions("custom_limits", 4);

        var quick1 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        quick1.Status.Should().Be(KifaActionStatus.OK);

        var quick2 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        quick2.Status.Should().Be(KifaActionStatus.OK);

        var quick3 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        quick3.Status.Should().Be(KifaActionStatus.Pending);
        quick3.Message.Should().Be("All 2 Quick sessions are reserved.");
    }

    [Fact]
    public void RenewSession_RenewsSessionWithCorrectLeaseDuration() {
        var account = CreateAccountWithSessions("renew_test", 2);

        var quick = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        quick.Status.Should().Be(KifaActionStatus.OK);

        var renewQuick = client.RenewSession(account.Id.Checked(), quick.Value.Checked().Id);
        renewQuick.Status.Should().Be(KifaActionStatus.OK);

        var updatedAccount = client.Get(account.Id.Checked()).Checked();
        var quickSession = updatedAccount.Sessions.Find(s => s.Id == quick.Value.Checked().Id).Checked();
        quickSession.Reserved.Should().BeCloseTo(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(5));

        var upload = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        upload.Status.Should().Be(KifaActionStatus.OK);

        var renewUpload = client.RenewSession(account.Id.Checked(), upload.Value.Checked().Id);
        renewUpload.Status.Should().Be(KifaActionStatus.OK);

        updatedAccount = client.Get(account.Id.Checked()).Checked();
        var uploadSession = updatedAccount.Sessions.Find(s => s.Id == upload.Value.Checked().Id).Checked();
        uploadSession.Reserved.Should().BeCloseTo(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ObtainSessionAsync_WhenAvailable_ReturnsImmediately() {
        var account = CreateAccountWithSessions("async_available", 2);

        var result = await client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);

        result.Status.Should().Be(KifaActionStatus.OK);
        result.Value.Should().NotBeNull();
        result.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
    }

    [Fact]
    public async Task ObtainSessionAsync_WhenFull_WaitsAndWakesUpOnRelease() {
        var account = CreateAccountWithSessions("async_wait_release", 1);

        var first = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        first.Status.Should().Be(KifaActionStatus.OK);
        var firstSessionId = first.Value.Checked().Id;

        var waitTask = client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);

        waitTask.IsCompleted.Should().BeFalse();

        var releaseResult = client.ReleaseSession(account.Id.Checked(), firstSessionId);
        releaseResult.Status.Should().Be(KifaActionStatus.OK);

        var second = await waitTask;
        second.Status.Should().Be(KifaActionStatus.OK);
        second.Value.Should().NotBeNull();
        second.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
    }

    [Fact]
    public async Task ObtainSessionAsync_MultipleWaiters_StrictFifoOrder() {
        var account = CreateAccountWithSessions("async_fifo", 1);

        var initial = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        initial.Status.Should().Be(KifaActionStatus.OK);
        var initialSessionId = initial.Value.Checked().Id;

        var waiter1 = client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);
        await Task.Delay(20);

        var waiter2 = client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);
        await Task.Delay(20);

        var waiter3 = client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);

        waiter1.IsCompleted.Should().BeFalse();
        waiter2.IsCompleted.Should().BeFalse();
        waiter3.IsCompleted.Should().BeFalse();

        // Release initial session -> waiter1 should complete
        client.ReleaseSession(account.Id.Checked(), initialSessionId);
        var res1 = await waiter1;
        res1.Status.Should().Be(KifaActionStatus.OK);
        waiter2.IsCompleted.Should().BeFalse();
        waiter3.IsCompleted.Should().BeFalse();

        // Release waiter1 session -> waiter2 should complete
        client.ReleaseSession(account.Id.Checked(), res1.Value.Checked().Id);
        var res2 = await waiter2;
        res2.Status.Should().Be(KifaActionStatus.OK);
        waiter3.IsCompleted.Should().BeFalse();

        // Release waiter2 session -> waiter3 should complete
        client.ReleaseSession(account.Id.Checked(), res2.Value.Checked().Id);
        var res3 = await waiter3;
        res3.Status.Should().Be(KifaActionStatus.OK);
    }

    [Fact]
    public async Task ObtainSessionAsync_WhenTimedOut_ReturnsPending() {
        var account = CreateAccountWithSessions("async_timeout", 1);

        var initial = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        initial.Status.Should().Be(KifaActionStatus.OK);

        client.WaitTimeout = TimeSpan.FromMilliseconds(50);
        var result = await client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);

        result.Status.Should().Be(KifaActionStatus.Pending);
        result.Message.Should().Contain("Timeout waiting for Quick session");
    }

    [Fact]
    public async Task ObtainSessionAsync_WhenCancelled_ThrowsAndFreesQueue() {
        var account = CreateAccountWithSessions("async_cancel", 1);

        var initial = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        initial.Status.Should().Be(KifaActionStatus.OK);
        var initialSessionId = initial.Value.Checked().Id;

        using var cts = new CancellationTokenSource();
        var waitTask = client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick, cancellationToken: cts.Token);

        cts.Cancel();

        var act = async () => await waitTask;
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Release the initial session. Since the waiter was cancelled, the session remains free for next obtain.
        client.ReleaseSession(account.Id.Checked(), initialSessionId);

        var next = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        next.Status.Should().Be(KifaActionStatus.OK);
    }

    [Fact]
    public async Task ObtainSessionAsync_DifferentTypes_NonConflictingProceeds() {
        TelegramAccount.MaxQuickSessions = 1;
        TelegramAccount.MaxUploadSessions = 2;
        var account = CreateAccountWithSessions("async_types", 2);

        // Reserve quick slot (1/1 full)
        var quick = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        quick.Status.Should().Be(KifaActionStatus.OK);

        // Quick is now full. A Quick waiter queues up.
        var quickWaiter = client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);
        quickWaiter.IsCompleted.Should().BeFalse();

        // An Upload request arrives. Since Upload limit (2) and account capacity (2 total, 1 free) has room,
        // it should succeed immediately without being blocked by quickWaiter.
        var uploadResult = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload);
        uploadResult.Status.Should().Be(KifaActionStatus.OK);
        uploadResult.Value.Checked().Type.Should().Be(TelegramSessionType.Upload);

        quickWaiter.IsCompleted.Should().BeFalse();

        // Release quick -> quick waiter wakes up
        client.ReleaseSession(account.Id.Checked(), quick.Value.Checked().Id);
        var quickResolved = await quickWaiter;
        quickResolved.Status.Should().Be(KifaActionStatus.OK);
    }

    [Fact]
    public void RenewSession_WithoutType_ReturnsBadRequest() {
        var account = CreateAccountWithSessions("renew_null_type", 1);
        var session = account.Sessions[0];
        session.Type = null;
        session.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5);
        client.Update(account);

        var result = client.RenewSession(account.Id.Checked(), session.Id);
        result.Status.Should().Be(KifaActionStatus.BadRequest);
        result.Message.Should().Contain("has no session type assigned");
    }

    [Fact]
    public void ObtainSession_InvalidSessionType_ThrowsException() {
        var account = CreateAccountWithSessions("invalid_type", 1);

        var act = () => client.ObtainSession(account.Id.Checked(), (TelegramSessionType) 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ObtainSessionAsync_WhenLeaseExpiresNaturally_WakesUpAndAllocatesSession() {
        TelegramAccount.MaxQuickSessions = 1;
        var account = CreateAccountWithSessions("lease_expire_test", 1);

        var session = account.Sessions[0];
        session.Type = TelegramSessionType.Quick;
        session.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(200);
        client.Update(account);

        var waiter = client.ObtainSessionAsync(account.Id.Checked(),
            TelegramSessionType.Quick);

        waiter.IsCompleted.Should().BeFalse();

        var result = await waiter;
        result.Status.Should().Be(KifaActionStatus.OK);
        result.Value.Should().NotBeNull();
        result.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
    }

    [Fact]
    public void ObtainSession_WhenColdestSessionNeedsRefreshAndFails_RelocatesToNextAvailableFreshSession() {
        var now = DateTimeOffset.UtcNow;
        var account = new TelegramAccount {
            Id = "relocate_test",
            Phone = "+1234567890",
            ApiId = 12345,
            ApiHash = "fake_hash",
            Sessions = new List<TelegramSession> {
                new() {
                    Id = 101,
                    Data = [1, 2, 3, 4],
                    Refreshed = now - TimeSpan.FromDays(2), // Needs refresh, will fail due to fake credentials
                    Reserved = Date.Zero
                },
                new() {
                    Id = 102,
                    Data = [1, 2, 3, 4],
                    Refreshed = now, // Fresh, no refresh needed
                    Reserved = Date.Zero.AddSeconds(1) // Slightly newer reserved timestamp
                }
            }
        };
        client.Set(account);

        var result = client.ObtainSession("relocate_test", TelegramSessionType.Quick);
        result.Status.Should().Be(KifaActionStatus.OK);
        result.Value.Should().NotBeNull();
        result.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);

        // Verify account state: session 101 marked as Refresh with future Reserved, session 102 leased
        var updatedAccount = client.Get("relocate_test").Checked();
        var session101 = updatedAccount.Sessions.First(s => s.Id == 101);
        session101.Type.Should().Be(TelegramSessionType.Refresh);
        session101.Reserved.Should().BeAfter(now);

        var leasedSession = updatedAccount.Sessions.First(s => s.Type == TelegramSessionType.Quick);
        leasedSession.Reserved.Should().BeAfter(now);
    }

    [Fact]
    public void ObtainSession_WhenAllSessionsFailRefresh_ReturnsPending() {
        var now = DateTimeOffset.UtcNow;
        var account = new TelegramAccount {
            Id = "all_fail_test",
            Phone = "+1234567890",
            ApiId = 12345,
            ApiHash = "fake_hash",
            Sessions = new List<TelegramSession> {
                new() {
                    Id = 201,
                    Data = [1, 2, 3, 4],
                    Refreshed = now - TimeSpan.FromDays(2),
                    Reserved = Date.Zero
                },
                new() {
                    Id = 202,
                    Data = [1, 2, 3, 4],
                    Refreshed = now - TimeSpan.FromDays(3),
                    Reserved = Date.Zero.AddSeconds(1)
                }
            }
        };
        client.Set(account);

        var result = client.ObtainSession("all_fail_test", TelegramSessionType.Quick);
        result.Status.Should().Be(KifaActionStatus.Pending);
        result.Message.Should().Be("All 2 sessions are reserved.");

        var updatedAccount = client.Get("all_fail_test").Checked();
        updatedAccount.Sessions.Should().OnlyContain(s => s.Type == TelegramSessionType.Refresh && s.Reserved > now);
    }

    [Fact]
    public void ObtainSession_WithSpecificSessionId_WhenItFailsRefresh_RelocatesToOtherFreshSession() {
        var now = DateTimeOffset.UtcNow;
        var account = new TelegramAccount {
            Id = "specific_session_relocate",
            Phone = "+1234567890",
            ApiId = 12345,
            ApiHash = "fake_hash",
            Sessions = new List<TelegramSession> {
                new() {
                    Id = 301,
                    Data = [1, 2, 3, 4],
                    Refreshed = now - TimeSpan.FromDays(2), // Needs refresh and fails
                    Reserved = Date.Zero
                },
                new() {
                    Id = 302,
                    Data = [1, 2, 3, 4],
                    Refreshed = now, // Fresh
                    Reserved = Date.Zero
                }
            }
        };
        client.Set(account);

        // Explicitly request session 301
        var result = client.ObtainSession("specific_session_relocate", TelegramSessionType.Quick, sessionId: 301);
        result.Status.Should().Be(KifaActionStatus.OK);
        result.Value.Should().NotBeNull();
        result.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);

        var updatedAccount = client.Get("specific_session_relocate").Checked();
        var session301 = updatedAccount.Sessions.First(s => s.Id == 301);
        session301.Type.Should().Be(TelegramSessionType.Refresh);
        session301.Reserved.Should().BeAfter(now);

        var leasedSession = updatedAccount.Sessions.First(s => s.Type == TelegramSessionType.Quick);
        leasedSession.Reserved.Should().BeAfter(now);
    }

    [Fact]
    public void RenewSession_WhenSessionTypeIsRefresh_ReturnsBadRequest() {
        var account = CreateAccountWithSessions("renew_refresh_type", 1);
        var session = account.Sessions[0];
        session.Type = TelegramSessionType.Refresh;
        session.Reserved = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5);
        client.Update(account);

        var result = client.RenewSession(account.Id.Checked(), session.Id);
        result.Status.Should().Be(KifaActionStatus.BadRequest);
        result.Message.Should().Contain("has no session type assigned");
    }

    [Fact]
    public void ReleaseSession_WithFloodWait_SetsReservedAndTypeRefresh() {
        var account = CreateAccountWithSessions("release_flood_wait", 1);
        var obtainResult = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        obtainResult.Status.Should().Be(KifaActionStatus.OK);
        var sessionId = obtainResult.Value.Checked().Id;

        var releaseResult = client.ReleaseSession(account.Id.Checked(), sessionId, floodWaitSeconds: 300);
        releaseResult.Status.Should().Be(KifaActionStatus.OK);

        var updatedAccount = client.Get(account.Id.Checked()).Checked();
        var session = updatedAccount.Sessions.First(s => s.Id == sessionId);
        session.Type.Should().Be(TelegramSessionType.Refresh);
        session.Reserved.Should().BeCloseTo(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(5));

        // When obtaining next session, since the only session is flooded, returns Pending
        var nextResult = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        nextResult.Status.Should().Be(KifaActionStatus.Pending);
        nextResult.Message.Should().Be("All 1 sessions are reserved.");
    }

    [Fact]
    public void ReleaseSession_WithFloodWait_AllowsOtherAvailableSessionToBeObtained() {
        var account = CreateAccountWithSessions("release_flood_wait_multi", 2);
        var obtainResult1 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        obtainResult1.Status.Should().Be(KifaActionStatus.OK);
        var session1Id = obtainResult1.Value.Checked().Id;

        // Release session 1 with flood wait
        var releaseResult = client.ReleaseSession(account.Id.Checked(), session1Id, floodWaitSeconds: 600);
        releaseResult.Status.Should().Be(KifaActionStatus.OK);

        // Obtain next session - should successfully lease session 2
        var obtainResult2 = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        obtainResult2.Status.Should().Be(KifaActionStatus.OK);
        obtainResult2.Value.Checked().Id.Should().NotBe(session1Id);
        obtainResult2.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
    }

    [Fact]
    public void ObtainSession_WithPreferredSessionId_ReleasesSameSessionWithoutRotatingId() {
        var account = CreateAccountWithSessions("sticky_session_test", 3);
        var initial = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        initial.Status.Should().Be(KifaActionStatus.OK);
        var sessionId = initial.Value.Checked().Id;

        // Release the session normally
        var releaseResult = client.ReleaseSession(account.Id.Checked(), sessionId);
        releaseResult.Status.Should().Be(KifaActionStatus.OK);

        // Re-obtain with preferred sessionId
        var reobtain = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Upload, sessionId: sessionId);
        reobtain.Status.Should().Be(KifaActionStatus.OK);
        reobtain.Value.Checked().Id.Should().Be(sessionId);
        reobtain.Value.Checked().Type.Should().Be(TelegramSessionType.Upload);
    }

    [Fact]
    public void ObtainSession_WithPreferredSessionId_WhenReserved_FallsBackToAnotherAvailableSession() {
        var account = CreateAccountWithSessions("sticky_fallback_test", 2);
        var first = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick);
        first.Status.Should().Be(KifaActionStatus.OK);
        var firstSessionId = first.Value.Checked().Id;

        // First session is still reserved (not released). Request with firstSessionId as preferred.
        var second = client.ObtainSession(account.Id.Checked(), TelegramSessionType.Quick, sessionId: firstSessionId);
        second.Status.Should().Be(KifaActionStatus.OK);
        second.Value.Checked().Id.Should().NotBe(firstSessionId);
        second.Value.Checked().Type.Should().Be(TelegramSessionType.Quick);
    }
}
