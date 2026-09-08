using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Kifa.Tests;

public class JsonExtensionsTests {
    class SampleModel {
        public string? MyProp { get; set; }
        public int NumberProp { get; set; }
    }

    [Fact]
    public void ToJsonAndFromJsonDefault() {
        var obj = new SampleModel {
            MyProp = "hello",
            NumberProp = 42
        };

        var json = obj.ToJson();
        json.Should().Be("{\"my_prop\":\"hello\",\"number_prop\":42}");

        var parsed = json.FromJson<SampleModel>();
        parsed.Should().NotBeNull();
        parsed!.MyProp.Should().Be("hello");
        parsed.NumberProp.Should().Be(42);
    }

    [Fact]
    public void ToPrettyJsonTest() {
        var obj = new SampleModel {
            MyProp = "hello",
            NumberProp = 42
        };

        var prettyJson = obj.ToPrettyJson();
        prettyJson.Should().Contain("\n");
        prettyJson.Should().Contain("  \"my_prop\": \"hello\"");

        var parsed = prettyJson.FromJson<SampleModel>();
        parsed.Should().NotBeNull();
        parsed!.MyProp.Should().Be("hello");
    }

    [Fact]
    public void ToCamelCaseJsonAndFromCamelCaseJson() {
        var obj = new SampleModel {
            MyProp = "world",
            NumberProp = 100
        };

        var camelJson = obj.ToCamelCaseJson();
        camelJson.Should().Be("{\"myProp\":\"world\",\"numberProp\":100}");

        var parsed = camelJson.FromCamelCaseJson<SampleModel>();
        parsed.Should().NotBeNull();
        parsed!.MyProp.Should().Be("world");
        parsed.NumberProp.Should().Be(100);
    }

    [Fact]
    public void FromJsonWithNullReturnsNull() {
        string? nullString = null;
        nullString.FromJson<SampleModel>().Should().BeNull();
        nullString.FromCamelCaseJson<SampleModel>().Should().BeNull();
        nullString.FromDiskJson<SampleModel>().Should().BeNull();
    }

    class NonNullableCollectionsModel {
        public List<string> List { get; set; } = new();
        public SortedSet<string> SortedSet { get; set; } = new();
        public HashSet<string> HashSet { get; set; } = new();
        public Dictionary<string, int> Dict { get; set; } = new();
        public string[] Array { get; set; } = [];
    }

    [Fact]
    public void NonNullableEmptyCollectionsAreOmitted() {
        var obj = new NonNullableCollectionsModel();
        obj.ToJson().Should().Be("{}");
    }

    [Fact]
    public void NonNullablePopulatedCollectionsAreSerialized() {
        var obj = new NonNullableCollectionsModel {
            List = ["a"],
            SortedSet = ["b"],
            HashSet = ["c"],
            Dict = new() {
                { "key", 1 }
            },
            Array = ["d"]
        };

        var json = obj.ToJson();
        json.Should().Contain("\"list\":[\"a\"]");
        json.Should().Contain("\"sorted_set\":[\"b\"]");
        json.Should().Contain("\"hash_set\":[\"c\"]");
        json.Should().Contain("\"dict\":{\"key\":1}");
        json.Should().Contain("\"array\":[\"d\"]");
    }

    class NullableCollectionsModel {
        public List<string>? List { get; set; }
        public SortedSet<string>? SortedSet { get; set; }
        public Dictionary<string, int>? Dict { get; set; }
    }

    [Fact]
    public void NullableCollectionsOmittedWhenNull() {
        var obj = new NullableCollectionsModel {
            List = null,
            SortedSet = null,
            Dict = null
        };
        obj.ToJson().Should().Be("{}");
    }

    [Fact]
    public void NullableCollectionsSerializedWhenEmpty() {
        var obj = new NullableCollectionsModel {
            List = new(),
            SortedSet = new(),
            Dict = new()
        };
        var json = obj.ToJson();
        json.Should().Contain("\"list\":[]");
        json.Should().Contain("\"sorted_set\":[]");
        json.Should().Contain("\"dict\":{}");
    }
}
