using System;
using System.Collections.Generic;
using Xunit;

namespace Kifa.Tests;

public class KifaTests {
    public static IEnumerable<object[]> Data
        => new List<object[]> {
            new object[] { new DateTime?[] { null, null }, 0 },
            new object[] { new DateTime?[] { null, DateTime.MinValue }, 1 },
            new object[] { new DateTime?[] { DateTime.MinValue, null }, 0 },
            new object[] { new DateTime?[] { DateTime.Today, DateTime.UnixEpoch }, 0 },
            new object[] { new DateTime?[] { DateTime.UnixEpoch, DateTime.Today }, 1 },
        };

    [Theory]
    [MemberData(nameof(Data))]
    public void MaxTimeTest(DateTime?[] arguments, int maxResult) {
        Assert.Equal(arguments[maxResult], Kifa.Max(arguments[0], arguments[1]));
        Assert.Equal(arguments[1 - maxResult], Kifa.Min(arguments[0], arguments[1]));
    }
}
