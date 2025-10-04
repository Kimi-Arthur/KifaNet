using System.Collections.Generic;
using Xunit;
using FluentAssertions;

namespace Kifa.Tests;

public class DifferTests {
    public static IEnumerable<object[]> Data
        => [
            new List<string>[] { ["a"], ["a"], ["  a"] },
            new List<string>[] { ["a"], ["b"], ["- a", "+ b"] },
            new List<string>[] { ["a", "a", "a"], ["a", "b", "a"], ["  a", "+ b", "  a", "- a"] },
            new List<string>[] {
                ["a", "b", "c", "d"], ["a", "b", "e", "d"], ["  a", "  b", "- c", "+ e", "  d"]
            },
        ];

    [Theory]
    [MemberData(nameof(Data))]
    public void DiffLinesTest(List<string> oldLines, List<string> newLines,
        List<string> finalLines) {
        LineDiffer.DiffLines(oldLines, newLines).Should().BeEquivalentTo(finalLines);
    }
}
