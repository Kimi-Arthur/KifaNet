using System;
using System.Collections.Generic;
using GlobExpressions;
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
        var selected = KifaCommand.ParseSelection("0-2", items, s => s);
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
        var selected = KifaCommand.ParseSelection("0-4,^2", items, s => s);
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
        var selected = KifaCommand.ParseSelection("0-2,5", items, s => s);
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
        var selected = KifaCommand.ParseSelection("0,3-", items, s => s);
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
        var selected = KifaCommand.ParseSelection("-2", items, s => s);
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
        var selected = KifaCommand.ParseSelection("0-1,4-5", items, s => s);
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
        var selected = KifaCommand.ParseSelection("0-5,^1-3", items, s => s);
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
        var selected = KifaCommand.ParseSelection("^1", items, s => s);
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
        var selected = KifaCommand.ParseSelection("0-3,^/*EP02*", items, s => s);
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
}
