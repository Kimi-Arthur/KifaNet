using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Kifa.Service;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliAccountTests {
    [Fact]
    public void CookieHelpersTest() {
        var cookies =
            "DedeUserID=3888766; SESSDATA=3fcd4e28%2C1798017359%2Cf42f1; bili_jct=old_csrf; buvid3=abc";

        BilibiliAccount.GetCookieValue(cookies, "bili_jct").Should().Be("old_csrf");
        BilibiliAccount.GetCookieValue(cookies, "DedeUserID").Should().Be("3888766");
        BilibiliAccount.GetCookieValue(cookies, "missing").Should().BeNull();

        var expiry = BilibiliAccount.GetSessDataExpiry(cookies);
        expiry.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1798017359));

        var merged = BilibiliAccount.MergeSetCookies(cookies, [
            "SESSDATA=new_sess%2C1898017359%2Cxyz; Domain=.bilibili.com; Path=/; HttpOnly",
            "bili_jct=new_csrf; Domain=.bilibili.com; Path=/",
            "sid=new_sid; Domain=.bilibili.com; Path=/"
        ]);

        BilibiliAccount.GetCookieValue(merged, "SESSDATA").Should()
            .Be("new_sess%2C1898017359%2Cxyz");
        BilibiliAccount.GetCookieValue(merged, "bili_jct").Should().Be("new_csrf");
        BilibiliAccount.GetCookieValue(merged, "DedeUserID").Should().Be("3888766");
        BilibiliAccount.GetCookieValue(merged, "buvid3").Should().Be("abc");
        BilibiliAccount.GetCookieValue(merged, "sid").Should().Be("new_sid");
    }

    [Fact]
    public void CorrespondPathAndGzipParseTest() {
        var path = CorrespondRpc.GetCorrespondPath(1794093019812);
        path.Should().HaveLength(256);
        path.Should().MatchRegex("^[0-9a-f]{256}$");

        var html = "<html><body><div id=\"1-name\">test_refresh_csrf_123</div></body></html>";
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true)) {
            gzip.Write(Encoding.UTF8.GetBytes(html));
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new ByteArrayContent(ms.ToArray())
        };
        var rpc = new CorrespondRpc(path);
        rpc.ParseResponse(response).Should().Be("test_refresh_csrf_123");
    }

    [Fact]
    public void GenWebTicketSignTest() {
        var rpc = new GenWebTicketRpc("csrf_123", "SESSDATA=abc", 1700000000);
        var request = rpc.GetRequest();
        request.RequestUri.Should().NotBeNull();
        request.RequestUri.Checked().ToString().Should().Be(
            "https://api.bilibili.com/bapis/bilibili.api.ticket.v1.Ticket/GenWebTicket?key_id=ec02&hexsign=bb79f0d980ffbb51597aa1a3e8b55603025cc1322ac766f4c1a98852e6182514&context[ts]=1700000000&csrf=csrf_123");
        request.Headers.GetValues("cookie").Single().Should().Be("SESSDATA=abc");
    }

    [Fact]
    public void FillRotatesSessDataAndTicketWhenRefreshNeededTest() {
        var requests = new List<string>();
        var handler = new FakeHttpMessageHandler(req => {
            var uri = req.RequestUri.Checked().ToString();
            requests.Add($"{req.Method} {uri}");

            if (uri.Contains("/x/passport-login/web/cookie/info")) {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(
                        """{"code":0,"message":"OK","ttl":1,"data":{"refresh":true,"timestamp":1794093019812}}""")
                };
            }

            if (uri.Contains("/correspond/1/")) {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent("""<div id="1-name">csrf_refresh_token_xyz</div>""")
                };
            }

            if (uri.Contains("/x/passport-login/web/cookie/refresh")) {
                var resp = new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(
                        """{"code":0,"message":"0","ttl":1,"data":{"status":0,"message":"","refresh_token":"new_refresh_token_456"}}""")
                };
                resp.Headers.Add("Set-Cookie",
                    "SESSDATA=new_sess%2C1900000000%2Cabc; Domain=.bilibili.com; Path=/; HttpOnly");
                resp.Headers.Add("Set-Cookie", "bili_jct=new_jct_789; Domain=.bilibili.com; Path=/");
                return resp;
            }

            if (uri.Contains("/x/passport-login/web/confirm/refresh")) {
                req.Headers.GetValues("cookie").Single().Should().Contain("bili_jct=new_jct_789");
                var body = req.Content.Checked().ReadAsStringAsync().Result;
                body.Should().Contain("csrf=new_jct_789");
                body.Should().Contain("refresh_token=old_refresh_token_123");
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent("""{"code":0,"message":"0","ttl":1}""")
                };
            }

            if (uri.Contains("/GenWebTicket")) {
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(
                        """{"code":0,"message":"OK","data":{"ticket":"new_jwt_ticket","created_at":1794000000,"ttl":259200}}""")
                };
            }

            throw new InvalidOperationException($"Unexpected URI: {uri}");
        });

        var oldAuthClient = BilibiliAccount.AuthClient;
        try {
            BilibiliAccount.AuthClient = new HttpClient(handler);
            var account = new BilibiliAccount {
                Id = "default",
                Cookies =
                    "DedeUserID=3888766; SESSDATA=old_sess%2C1798017359%2Cdef; bili_jct=old_jct_000; bili_ticket=old_ticket",
                RefreshToken = "old_refresh_token_123"
            };

            account.Fill();

            account.RefreshToken.Should().Be("new_refresh_token_456");
            BilibiliAccount.GetCookieValue(account.Cookies, "SESSDATA").Should()
                .Be("new_sess%2C1900000000%2Cabc");
            BilibiliAccount.GetCookieValue(account.Cookies, "bili_jct").Should().Be("new_jct_789");
            BilibiliAccount.GetCookieValue(account.Cookies, "bili_ticket").Should()
                .Be("new_jwt_ticket");
            BilibiliAccount.GetCookieValue(account.Cookies, "bili_ticket_expires").Should()
                .Be("1794259200");
            requests.Should().HaveCount(5);
        } finally {
            BilibiliAccount.AuthClient = oldAuthClient;
        }
    }

    [Fact]
    public void HttpClientsSyncAndRefreshCookiesTest() {
        var oldClient = BilibiliAccount.Client;
        var oldAccountId = BilibiliAccount.AccountId;
        try {
            BilibiliAccount.Client = new FakeBilibiliAccountClient();
            BilibiliAccount.AccountId = "test-acc";
            HttpClients.CookieSyncInterval = TimeSpan.Zero;

            HttpClients.GetActiveBilibiliCookies().Should()
                .Be("SESSDATA=initial_sess; bili_jct=initial_jct");
            HttpClients.BilibiliCsrfToken.Should().Be("initial_jct");

            HttpClients.RefreshBilibiliCookies();
            HttpClients.GetActiveBilibiliCookies().Should()
                .Be("SESSDATA=rotated_sess; bili_jct=rotated_jct");
            HttpClients.BilibiliCsrfToken.Should().Be("rotated_jct");
        } finally {
            BilibiliAccount.Client = oldClient;
            BilibiliAccount.AccountId = oldAccountId;
            HttpClients.CookieSyncInterval = TimeSpan.FromMinutes(1);
        }
    }

    [Fact]
    public void LiveAccountCookieInfoCorrespondAndTicketRefreshTest() {
        KifaConfigs.Init("/etc/kimily/common.yaml");
        var cookies = HttpClients.BilibiliCookies.Checked();
        var csrf = BilibiliAccount.GetCookieValue(cookies, "bili_jct").Checked();

        var cookieInfo = BilibiliAccount.AuthClient.Call(new CookieInfoRpc(csrf, cookies));
        cookieInfo.Should().NotBeNull();
        cookieInfo.Checked().Code.Should().Be(0);
        cookieInfo.Data.Should().NotBeNull();
        cookieInfo.Data.Checked().Timestamp.Should().BeGreaterThan(0);

        var refreshCsrf = BilibiliAccount.AuthClient.Call(
            new CorrespondRpc(cookieInfo.Data.Timestamp, cookies));
        refreshCsrf.Should().NotBeNullOrEmpty();
        refreshCsrf.Should().MatchRegex("^[0-9a-f]{32}$");

        var account = new BilibiliAccount {
            Id = "default",
            Cookies = cookies
        };
        account.Fill();

        var newTicket = BilibiliAccount.GetCookieValue(account.Cookies, "bili_ticket");
        newTicket.Should().NotBeNullOrEmpty();
        newTicket.Should().StartWith("eyJ");

        var qrCode = BilibiliAccount.AuthClient.Call(new QrCodeGenerateRpc());
        qrCode.Should().NotBeNull();
        qrCode.Checked().Code.Should().Be(0);
        qrCode.Data?.QrcodeKey.Should().NotBeNullOrEmpty();
    }

    class FakeBilibiliAccountClient : BaseKifaServiceClient<BilibiliAccount> {
        string cookies = "SESSDATA=initial_sess; bili_jct=initial_jct";

        public override BilibiliAccount? Get(string id, KifaDataOptions? options = null) {
            if (options?.Refresh == true) {
                cookies = "SESSDATA=rotated_sess; bili_jct=rotated_jct";
            }

            return new BilibiliAccount {
                Id = id,
                Cookies = cookies
            };
        }

        public override List<BilibiliAccount?> Get(List<string> ids, KifaDataOptions? options = null)
            => throw new NotImplementedException();

        public override SortedDictionary<string, BilibiliAccount> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => throw new NotImplementedException();

        public override KifaActionResult Set(BilibiliAccount data)
            => throw new NotImplementedException();

        public override KifaActionResult Set(List<BilibiliAccount> data)
            => throw new NotImplementedException();

        public override KifaActionResult Update(BilibiliAccount data)
            => throw new NotImplementedException();

        public override KifaActionResult Update(List<BilibiliAccount> data)
            => throw new NotImplementedException();

        public override KifaActionResult Delete(string id)
            => throw new NotImplementedException();

        public override KifaActionResult Delete(List<string> ids)
            => throw new NotImplementedException();

        public override KifaActionResult Link(string targetId, string linkId)
            => throw new NotImplementedException();
    }

    class FakeHttpMessageHandler : HttpMessageHandler {
        readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) {
            this.responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responder(request));

        protected override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responder(request);
    }
}
