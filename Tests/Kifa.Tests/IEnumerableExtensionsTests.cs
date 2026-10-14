using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Kifa.Tests;

public class IEnumerableExtensionsTests {
    [Fact]
    public void ExceptNull_Class_FiltersNullValues() {
        var items = new List<string?> { "a", null, "b", null, "c" };
        var result = items.ExceptNull().ToList();

        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void ExceptNull_Struct_FiltersNullValues() {
        var items = new List<int?> { 1, null, 2, null, 3 };
        var result = items.ExceptNull().ToList();

        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ExceptNull_DateTimeOffsetStruct_FiltersNullValues() {
        var now = DateTimeOffset.UtcNow;
        var items = new List<DateTimeOffset?> { now, null, now.AddMinutes(1) };
        var result = items.ExceptNull().ToList();

        result.Should().Equal(now, now.AddMinutes(1));
    }

    [Fact]
    public void OnlyNonNull_Struct_ReturnsNonNullableValues() {
        var items = new List<int?> { 1, 2, 3 };
        var result = items.OnlyNonNull().ToList();

        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void OnlyNonNull_Struct_ThrowsOnNull() {
        var items = new List<int?> { 1, null, 3 };
        var act = () => items.OnlyNonNull().ToList();

        act.Should().Throw<ArgumentNullException>();
    }
}
