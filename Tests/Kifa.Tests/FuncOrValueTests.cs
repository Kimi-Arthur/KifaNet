using System;
using Xunit;

namespace Kifa.Tests;

public class FuncOrValueTests {
    [Fact]
    public void FuncTest() {
        var s = "a";
        FuncOrValue<string> f = new Func<string>(() => s += s);
        Assert.Equal("aa", f.Get());
        Assert.Equal("aaaa", f.Get());
    }

    [Fact]
    public void ValueTest() {
        var s = "a";
        FuncOrValue<string> f = s;
        s += s;
        Assert.Equal("a", f.Get());
        Assert.Equal("a", f.Get());
    }
}
