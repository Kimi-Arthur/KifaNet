using System;
using System.Collections.Generic;
using GlobExpressions;
using Kifa.Jobs;
using Kifa.Service;
using Xunit;

namespace Kifa.Tools.Tests;

public class SelectManySelectionTests {
    [Fact]
    public void ParseSelection_NumericRange() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4"
        };
        var selected = KifaCommand.ParseSelection("1-3", items, s => s);
        Assert.Equal(new[] { 0, 1, 2 }, selected);
    }

    [Fact]
    public void ParseSelection_NumericRange_ZeroBased() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4"
        };
        var selected = KifaCommand.ParseSelection("0-2", items, s => s, startingIndex: 0);
        Assert.Equal(new[] { 0, 1, 2 }, selected);
    }

    [Fact]
    public void ParseSelection_NumericExclusion() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4"
        };
        var selected = KifaCommand.ParseSelection("1-5,^3", items, s => s);
        Assert.Equal(new[] { 0, 1, 3, 4 }, selected);
    }

    [Fact]
    public void ParseSelection_NumericRangeAndSingleCombination() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4",
            "item5"
        };
        var selected = KifaCommand.ParseSelection("1-3,6", items, s => s);
        Assert.Equal(new[] { 0, 1, 2, 5 }, selected);
    }

    [Fact]
    public void ParseSelection_SingleAndOpenEndedRangeCombination() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4",
            "item5"
        };
        var selected = KifaCommand.ParseSelection("1,4-", items, s => s);
        Assert.Equal(new[] { 0, 3, 4, 5 }, selected);
    }

    [Fact]
    public void ParseSelection_OpenStartRange() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4",
            "item5"
        };
        var selected = KifaCommand.ParseSelection("-3", items, s => s);
        Assert.Equal(new[] { 0, 1, 2 }, selected);
    }

    [Fact]
    public void ParseSelection_MultipleNumericRanges() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4",
            "item5"
        };
        var selected = KifaCommand.ParseSelection("1-2,5-6", items, s => s);
        Assert.Equal(new[] { 0, 1, 4, 5 }, selected);
    }

    [Fact]
    public void ParseSelection_RangeExclusion() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4",
            "item5"
        };
        var selected = KifaCommand.ParseSelection("1-6,^2-4", items, s => s);
        Assert.Equal(new[] { 0, 4, 5 }, selected);
    }

    [Fact]
    public void ParseSelection_SingleExclusion() {
        var items = new List<string> {
            "item0",
            "item1",
            "item2",
            "item3",
            "item4"
        };
        var selected = KifaCommand.ParseSelection("^2", items, s => s);
        Assert.Equal(new[] { 0, 2, 3, 4 }, selected);
    }

    [Fact]
    public void ParseSelection_SingleGlob() {
        var items = new List<string> {
            "EP01.mp4",
            "EP02.mp4",
            "Trailer.mp4"
        };
        var selected = KifaCommand.ParseSelection("/*EP[0-9]*", items, s => s);
        Assert.Equal(new[] { 0, 1 }, selected);
    }

    [Fact]
    public void ParseSelection_InvertedGlob() {
        var items = new List<string> {
            "EP01.mp4",
            "EP02.mp4",
            "Trailer.mp4"
        };
        var selected = KifaCommand.ParseSelection("^/*Trailer*", items, s => s);
        Assert.Equal(new[] { 0, 1 }, selected);
    }

    [Fact]
    public void ParseSelection_MultipleGlobsCombination() {
        var items = new List<string> {
            "EP01.mp4",
            "EP02.mp4",
            "Trailer.mp4"
        };
        var selected = KifaCommand.ParseSelection("/*EP01*,/*Trailer*", items, s => s);
        Assert.Equal(new[] { 0, 2 }, selected);
    }

    [Fact]
    public void ParseSelection_MultipleGlobsWithInversion() {
        var items = new List<string> {
            "EP01.mp4",
            "EP02.mp4",
            "Trailer.mp4"
        };
        var selected = KifaCommand.ParseSelection("/*EP*,^/*EP02*", items, s => s);
        Assert.Equal(new[] { 0 }, selected);
    }

    [Fact]
    public void ParseSelection_PosixNegationGlob() {
        var items = new List<string> {
            "EP01.mp4",
            "EPTrailer.mp4",
            "EP02.mp4"
        };
        var selected = KifaCommand.ParseSelection("/*EP[!0-9]*", items, s => s);
        Assert.Equal(new[] { 1 }, selected);
    }

    [Fact]
    public void ParseSelection_MixedRangeAndInvertedGlob() {
        var items = new List<string> {
            "EP01.mp4",
            "EP02.mp4",
            "EP03.mp4",
            "EP04.mp4"
        };
        var selected = KifaCommand.ParseSelection("1-4,^/*EP02*", items, s => s);
        Assert.Equal(new[] { 0, 2, 3 }, selected);
    }

    [Fact]
    public void ParseSelection_OutOfBoundsIndex_ThrowsException() {
        var items = new List<string> {
            "EP01.mp4",
            "EP02.mp4"
        };
        Assert.Throws<ArgumentOutOfRangeException>(()
            => KifaCommand.ParseSelection("999", items, s => s));
    }

    [Fact]
    public void ParseSelection_GlobEndingInA() {
        var items = new List<string> {
            "EP01a.mp4",
            "EP02b.mp4",
            "Trailer.mp4"
        };
        var selected = KifaCommand.ParseSelection("/*EP01a*", items, s => s);
        Assert.Equal(new[] { 0 }, selected);
    }

    [Fact]
    public void ParseSelection_InvalidGlob_ThrowsException() {
        var items = new List<string> {
            "EP01.mp4"
        };
        Assert.Throws<GlobPatternException>(()
            => KifaCommand.ParseSelection("/[abc", items, s => s));
    }

    class DummyCommand : KifaCommand {
        public override int Execute(KifaTask? task = null) => 0;

        public KifaActionResult<List<string>> TestSelectMany(List<string> choices, string selectionKey) {
            return SelectMany(choices, s => s, selectionKey: selectionKey);
        }

        public KifaActionResult<(string Choice, int? Part, int Index, bool Special)> TestSelectOne(
            List<string> choices, string selectionKey) {
            return SelectOne(choices, s => s, selectionKey: selectionKey);
        }
    }

    [Fact]
    public void SelectOne_DefaultChoice() {
        var originalIn = Console.In;
        try {
            var key = $"test_key_{Guid.NewGuid()}";
            Console.SetIn(new System.IO.StringReader("\n"));

            var cmd = new DummyCommand();
            var res = cmd.TestSelectOne(new List<string> { "a", "b", "c" }, key);
            Assert.Equal("a", res.Response.Choice);
            Assert.Equal(0, res.Response.Index);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void SelectOne_PrefixAlwaysFlag() {
        var originalIn = Console.In;
        try {
            var key = $"test_key_{Guid.NewGuid()}";
            Console.SetIn(new System.IO.StringReader("a2\n"));

            var cmd = new DummyCommand();
            var res1 = cmd.TestSelectOne(new List<string> { "a", "b", "c" }, key);
            Assert.Equal("b", res1.Response.Choice);

            var res2 = cmd.TestSelectOne(new List<string> { "x", "y", "z" }, key);
            Assert.Equal("y", res2.Response.Choice);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void SelectOne_PrefixAlwaysDefaultFlag() {
        var originalIn = Console.In;
        try {
            var key = $"test_key_{Guid.NewGuid()}";
            Console.SetIn(new System.IO.StringReader("a\n"));

            var cmd = new DummyCommand();
            var res1 = cmd.TestSelectOne(new List<string> { "a", "b", "c" }, key);
            Assert.Equal("a", res1.Response.Choice);

            var res2 = cmd.TestSelectOne(new List<string> { "x", "y", "z" }, key);
            Assert.Equal("x", res2.Response.Choice);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void SelectOne_Ignore() {
        var originalIn = Console.In;
        try {
            var key = $"test_key_{Guid.NewGuid()}";
            Console.SetIn(new System.IO.StringReader("^\n"));

            var cmd = new DummyCommand();
            var res = cmd.TestSelectOne(new List<string> { "a", "b", "c" }, key);
            Assert.Equal(KifaActionStatus.Skipped, res.Status);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void SelectMany_RemembersPreviousSelectionAsDefault() {
        var originalIn = Console.In;
        try {
            var key = $"test_key_{Guid.NewGuid()}";
            Console.SetIn(new System.IO.StringReader("1-2\n\n"));

            var cmd = new DummyCommand();
            var res1 = cmd.TestSelectMany(new List<string> { "a", "b", "c" }, key);
            Assert.Equal(new[] { "a", "b" }, res1.Response);

            var res2 = cmd.TestSelectMany(new List<string> { "x", "y", "z" }, key);
            Assert.Equal(new[] { "x", "y" }, res2.Response);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void SelectMany_AlwaysDefaultFlag() {
        var originalIn = Console.In;
        try {
            var key = $"test_key_{Guid.NewGuid()}";
            Console.SetIn(new System.IO.StringReader("a1-2\n"));

            var cmd = new DummyCommand();
            var res1 = cmd.TestSelectMany(new List<string> { "a", "b", "c" }, key);
            Assert.Equal(new[] { "a", "b" }, res1.Response);

            var res2 = cmd.TestSelectMany(new List<string> { "x", "y", "z" }, key);
            Assert.Equal(new[] { "x", "y" }, res2.Response);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void SelectMany_AllKeywordSelectsAll() {
        var originalIn = Console.In;
        try {
            var key = $"test_key_{Guid.NewGuid()}";
            Console.SetIn(new System.IO.StringReader("all\n"));

            var cmd = new DummyCommand();
            var res = cmd.TestSelectMany(new List<string> { "a", "b", "c" }, key);
            Assert.Equal(new[] { "a", "b", "c" }, res.Response);
        } finally {
            Console.SetIn(originalIn);
        }
    }
}
