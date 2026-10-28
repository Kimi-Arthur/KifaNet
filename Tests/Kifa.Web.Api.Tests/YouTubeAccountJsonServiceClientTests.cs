using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kifa.Service;
using Kifa.Web.Api.Controllers;
using Kifa.YouTube;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class YouTubeAccountJsonServiceClientTests : IDisposable {
    readonly string folder;
    readonly YouTubeAccountJsonServiceClient client = new();
    readonly KifaServiceJsonClient<YouTubeSession> sessionClient = new();

    public YouTubeAccountJsonServiceClientTests() {
        folder =
            $"{Path.GetTempPath()}/{nameof(YouTubeAccountJsonServiceClientTests)}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next()}";
        client.DataFolder = folder;
        sessionClient.DataFolder = folder;
        YouTubeAccount.MaxQuickSessions = 5;
        YouTubeAccount.MaxDownloadSessions = 2;
        YouTubeAccount.Client = client;
    }

    public void Dispose() {
        YouTubeAccount.MaxQuickSessions = 5;
        YouTubeAccount.MaxDownloadSessions = 2;
        YouTubeAccount.Client = new YouTubeAccount.RestServiceClient();
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void AddSession_CreatesSessionDataModelAndExternalCookieFileAndLinksInAccount() {
        var account = new YouTubeAccount {
            Id = "Kimily",
            Username = "test@example.com"
        };
        client.Set(account).Status.Should().Be(KifaActionStatus.OK);

        client.AddSession("Kimily",
                "# Netscape HTTP Cookie File\n.youtube.com\tTRUE\t/\tTRUE\t0\tSID\tsession0\n")
            .Status.Should().Be(KifaActionStatus.OK);
        client.AddSession("Kimily",
                "# Netscape HTTP Cookie File\n.youtube.com\tTRUE\t/\tTRUE\t0\tSID\tsession1\n")
            .Status.Should().Be(KifaActionStatus.OK);

        var accountJsonPath = $"{folder}/youtube/accounts/Kimily.json";
        var session0JsonPath = $"{folder}/youtube/sessions/Kimily/0.json";
        var session0CookiePath = $"{folder}/youtube/sessions/Kimily/0.cookies.txt";
        var session1JsonPath = $"{folder}/youtube/sessions/Kimily/1.json";
        var session1CookiePath = $"{folder}/youtube/sessions/Kimily/1.cookies.txt";

        File.Exists(accountJsonPath).Should().BeTrue();
        File.Exists(session0JsonPath).Should().BeTrue();
        File.Exists(session0CookiePath).Should().BeTrue();
        File.Exists(session1JsonPath).Should().BeTrue();
        File.Exists(session1CookiePath).Should().BeTrue();

        var accountRawJson = File.ReadAllText(accountJsonPath);
        accountRawJson.Should().Contain("Kimily/0");
        accountRawJson.Should().Contain("Kimily/1");
        accountRawJson.Should().NotContain("cookie");

        var session0RawJson = File.ReadAllText(session0JsonPath);
        session0RawJson.Should().NotContain("Netscape HTTP Cookie File");
        session0RawJson.Should().NotContain("cookie");
        session0RawJson.Should().Contain("$metadata");
        session0RawJson.Should().Contain("version");

        File.ReadAllText(session0CookiePath).Should().Contain("session0");
        File.ReadAllText(session1CookiePath).Should().Contain("session1");

        var loadedSession0 = sessionClient.Get("Kimily/0").Checked();
        loadedSession0.Cookies.Should().Contain("session0");
        loadedSession0.Metadata.Checked().Version.Should().NotBeNull();
        loadedSession0.Metadata.Checked().Version.Checked().Value.Should().BeAfter(Date.Zero);
    }

    [Fact]
    public void ObtainAndReleaseSession_RotatesCookieAndNotifiesWaiterWithUpdatedCookie() {
        var account = new YouTubeAccount {
            Id = "RotateTest"
        };
        client.Set(account);
        client.AddSession("RotateTest", "initial_cookie");

        var first = client.ObtainSession("RotateTest", YouTubeSessionType.Quick);
        first.Status.Should().Be(KifaActionStatus.OK);
        first.Value.Checked().Id.Should().Be("RotateTest/0");
        first.Value.Checked().Cookies.Should().Be("initial_cookie");
        var initialVersion = first.Value.Checked().Metadata.Checked().Version;
        initialVersion.Should().NotBeNull();
        var sessionId = first.Value.Checked().Id.Checked();

        Thread.Sleep(10);

        // Update with identical cookies: Version stays same, LastRefreshed advances
        client.UpdateSession("RotateTest", sessionId, "initial_cookie").Status.Should()
            .Be(KifaActionStatus.OK);
        var afterSameUpdate = sessionClient.Get(sessionId).Checked();
        afterSameUpdate.Metadata.Checked().Version.Should().Be(initialVersion);
        afterSameUpdate.Metadata.Checked().LastRefreshed.Checked().Value.Should()
            .BeAfter(initialVersion.Checked().Value);

        Thread.Sleep(10);

        var waiterTask = client.ObtainSessionAsync("RotateTest", YouTubeSessionType.Quick);
        waiterTask.IsCompleted.Should().BeFalse();

        // Release with rotated cookie: Version advances and matches LastRefreshed
        var releaseResult = client.ReleaseSession("RotateTest", sessionId, "rotated_cookie_v2");
        releaseResult.Status.Should().Be(KifaActionStatus.OK);

        var second = waiterTask.GetAwaiter().GetResult();
        second.Status.Should().Be(KifaActionStatus.OK);
        second.Value.Checked().Cookies.Should().Be("rotated_cookie_v2");
        second.Value.Checked().Metadata.Checked().Version.Checked().Value.Should()
            .BeAfter(afterSameUpdate.Metadata.Checked().LastRefreshed.Checked().Value);
        second.Value.Checked().Metadata.Checked().LastRefreshed.Should()
            .Be(second.Value.Checked().Metadata.Checked().Version);

        File.ReadAllText($"{folder}/youtube/sessions/RotateTest/0.cookies.txt")
            .Should().Be("rotated_cookie_v2");
        File.ReadAllText($"{folder}/youtube/sessions/RotateTest/0.json")
            .Should().NotContain("last_refreshed");
    }

    [Fact]
    public async Task ObtainSessionAsync_WhenServerStopping_ReturnsPendingImmediatelyAndFreesWaiter() {
        var account = new YouTubeAccount {
            Id = "StoppingTest"
        };
        client.Set(account);
        client.AddSession("StoppingTest", "initial_cookie");

        var first = client.ObtainSession("StoppingTest", YouTubeSessionType.Quick);
        first.Status.Should().Be(KifaActionStatus.OK);
        var sessionId = first.Value.Checked().Id.Checked();

        using var stoppingCts = new CancellationTokenSource();
        var waitTask = client.ObtainSessionAsync("StoppingTest", YouTubeSessionType.Quick, null,
            default, stoppingCts.Token);
        waitTask.IsCompleted.Should().BeFalse();

        stoppingCts.Cancel();

        var stoppedResult = await waitTask;
        stoppedResult.Status.Should().Be(KifaActionStatus.Pending);
        stoppedResult.Message.Should().Contain("Server is stopping");

        client.ReleaseSession("StoppingTest", sessionId).Status.Should().Be(KifaActionStatus.OK);
        var next = client.ObtainSession("StoppingTest", YouTubeSessionType.Quick);
        next.Status.Should().Be(KifaActionStatus.OK);
    }

    [Fact]
    public void ObtainLease_CreatesTempCookieFileAndReleasesUpdatedCookiesOnDispose() {
        var account = new YouTubeAccount {
            Id = "LeaseTest"
        };
        client.Set(account);
        client.AddSession("LeaseTest", "initial_cookie_content");

        string? tempCookiePath = null;
        using (var lease = YouTubeAccount.ObtainLease(YouTubeSessionType.Quick, "LeaseTest")) {
            lease.Should().NotBeNull();
            tempCookiePath = lease!.CookieFile;
            tempCookiePath.Should().NotBeNull();
            File.Exists(tempCookiePath).Should().BeTrue();
            File.ReadAllText(tempCookiePath!).Should().Be("initial_cookie_content");

            // Update cookie file as yt-dlp would
            File.WriteAllText(tempCookiePath!, "updated_cookie_from_ytdlp");
        }

        // After dispose: temp file deleted and session released with updated cookies
        File.Exists(tempCookiePath).Should().BeFalse();

        var reobtained = client.ObtainSession("LeaseTest", YouTubeSessionType.Quick);
        reobtained.Status.Should().Be(KifaActionStatus.OK);
        reobtained.Value.Checked().Cookies.Should().Be("updated_cookie_from_ytdlp");
    }

    [Fact]
    public async Task YouTubeSessionLease_RenewsReservationPeriodically() {
        var account = new YouTubeAccount {
            Id = "RenewTest"
        };
        client.Set(account);
        client.AddSession("RenewTest", "renew_cookie");

        YouTubeSessionLease.QuickKeepSessionInterval = TimeSpan.FromMilliseconds(50);
        try {
            using var lease = YouTubeAccount.ObtainLease(YouTubeSessionType.Quick, "RenewTest");
            lease.Should().NotBeNull();

            var initialAccount = client.Get("RenewTest").Checked();
            var initialReserved = initialAccount.Sessions[0].Reserved;

            await Task.Delay(150);

            var refreshedAccount = client.Get("RenewTest").Checked();
            refreshedAccount.Sessions[0].Reserved.Should().BeAfter(initialReserved);
        } finally {
            YouTubeSessionLease.QuickKeepSessionInterval = TimeSpan.FromMinutes(1);
        }
    }
}
