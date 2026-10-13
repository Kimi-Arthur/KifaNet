using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Tests;

class MyJson : JsonSerializable {
    public required string Code { get; set; }
    public required string Code2 { get; set; }

    public string ToJson() => Code;

    public static implicit operator MyJson(string id)
        => new() {
            Code = id,
            Code2 = id[..2]
        };
}

public class JsonSerializationTests {
    [Fact]
    public void BasicDeserializationTest() {
        var x = JsonConvert.DeserializeObject<Dictionary<string, MyJson>>("{\"good\":\"ok2\"}",
            KifaJsonSerializerSettings.Default);

        Assert.Equal("ok2", x["good"].Code);
        Assert.Equal("ok", x["good"].Code2);
    }

    [Fact]
    public void BasicSerializationTest() {
        var x = JsonConvert.SerializeObject(new MyJson {
            Code = "123",
            Code2 = "23"
        }, KifaJsonSerializerSettings.Default);

        Assert.Equal("\"123\"", x);
    }

    [Fact]
    public void DateTimeOffsetSerializationTest() {
        var dto = new DateTimeOffset(2026, 10, 13, 14, 15, 0, 123, 456, TimeSpan.FromHours(2));
        var json = JsonConvert.SerializeObject(new Dictionary<string, DateTimeOffset> {
            { "time", dto }
        }, KifaJsonSerializerSettings.Default);

        // Should serialize the UTC equivalent: 12:15:00.123456
        Assert.Equal("{\"time\":\"2026-10-13 12:15:00.123456\"}", json);

        var deserialized = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset>>(json,
            KifaJsonSerializerSettings.Default).Checked();
        Assert.Equal(TimeSpan.Zero, deserialized["time"].Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 13, 12, 15, 0, 123, 456, TimeSpan.Zero),
            deserialized["time"]);

        // Formats test
        var variousFormats = new[] {
            "\"2026-10-13 12:15:00.123456\"",
            "\"2026-10-13 12:15:00\"",
            "\"2026-10-13T12:15:00.123456Z\"",
            "\"2026-10-13T14:15:00+02:00\"",
            "\"2026-10-13\"",
            "\"20261013121500123456\"",
            "\"20261013\""
        };
        foreach (var fmt in variousFormats) {
            var parsed = JsonConvert.DeserializeObject<DateTimeOffset>(fmt, KifaJsonSerializerSettings.Default);
            Assert.Equal(TimeSpan.Zero, parsed.Offset);
        }

        // Nullable test
        var nullableJson = "{\"present\":\"2026-10-13 12:15:00.123456\",\"missing\":null}";
        var nullableDict = JsonConvert.DeserializeObject<Dictionary<string, DateTimeOffset?>>(
            nullableJson, KifaJsonSerializerSettings.Default).Checked();
        Assert.Equal(TimeSpan.Zero, nullableDict["present"].Checked().Offset);
        Assert.Null(nullableDict["missing"]);
    }
}
