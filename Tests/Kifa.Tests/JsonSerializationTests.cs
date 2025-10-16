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
}
