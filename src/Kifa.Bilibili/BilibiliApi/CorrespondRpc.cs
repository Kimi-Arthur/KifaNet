using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kifa.Rpc;

namespace Kifa.Bilibili.BilibiliApi;

// Reference: https://github.com/SocialSisterYi/bilibili-API-collect/blob/master/docs/login/cookie_refresh.md
public sealed class CorrespondRpc : KifaParameterizedRpc, KifaRpc<string?> {
    protected override string Url => "https://www.bilibili.com/correspond/1/{correspond_path}";

    protected override HttpMethod Method => HttpMethod.Get;

    protected override Dictionary<string, string> Headers
        => Parameters.ContainsKey("cookies")
            ? new() {
                { "cookie", "{cookies}" }
            }
            : new();

    const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDLgd2OAkcGVtoE3ThUREbio0Eg
        Uc/prcajMKXvkCKFCWhJYJcLkcM2DKKcSeFpD/j6Boy538YXnR6VhcuUJOhH2x71
        nzPjfdTcqMz7djHum0qSZA0AyCBDABUqCrfNgCiJ00Ra7GmRj+YCK1NJEuewlb40
        JNrRuoEUXpabUzGB8QIDAQAB
        -----END PUBLIC KEY-----
        """;

    public static string GetCorrespondPath(long timestamp) {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(PublicKeyPem);
        var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes($"refresh_{timestamp}"),
            RSAEncryptionPadding.OaepSHA256);
        return Convert.ToHexString(encrypted).ToLowerInvariant();
    }

    public CorrespondRpc(long timestamp, string? cookies = null) : this(
        GetCorrespondPath(timestamp), cookies) {
    }

    public CorrespondRpc(string correspondPath, string? cookies = null) {
        Parameters = new() {
            { "correspond_path", correspondPath }
        };

        if (cookies != null) {
            Parameters["cookies"] = cookies;
        }
    }

    static readonly Regex RefreshCsrfPattern = new(@"<div id=""1-name"">([^<]+)</div>");

    public string? ParseResponse(HttpResponseMessage responseMessage) {
        using (responseMessage) {
            responseMessage.EnsureSuccessStatusCode();
            var bytes = responseMessage.Content.ReadAsByteArrayAsync().Result;
            string html;
            if (bytes.Length >= 2 && bytes[0] == 0x1f && bytes[1] == 0x8b) {
                using var ms = new MemoryStream(bytes);
                using var gzip = new GZipStream(ms, CompressionMode.Decompress);
                using var sr = new StreamReader(gzip, Encoding.UTF8);
                html = sr.ReadToEnd();
            } else {
                html = Encoding.UTF8.GetString(bytes);
            }

            var match = RefreshCsrfPattern.Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
