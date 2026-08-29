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
    }
}
