using System;
using FluentAssertions;
using Kifa.Tools.DataUtil.Commands;
using OpenQA.Selenium;
using Xunit;

namespace Kifa.Tools.Tests;

public class AddCommandYouTubeAccountTests {
    [Fact]
    public void ToNetscapeCookieFormat_FormatsCookiesCorrectly() {
        var expiry = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expiryUnix = new DateTimeOffset(expiry).ToUnixTimeSeconds();
        var cookies = new[] {
            new Cookie("SID", "session_val", ".youtube.com", "/", expiry, true, true, null),
            new Cookie("PREF", "f1=50000000", "youtube.com", "/path", null, false, false, null)
        };

        var result = AddCommand.ToNetscapeCookieFormat(cookies);
        result.Should().StartWith("# Netscape HTTP Cookie File\n");
        result.Should().Contain($".youtube.com\tTRUE\t/\tTRUE\t{expiryUnix}\tSID\tsession_val\n");
        result.Should().Contain("youtube.com\tFALSE\t/path\tFALSE\t0\tPREF\tf1=50000000\n");
    }
}
