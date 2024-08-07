using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Tests;

public enum MyEnum {
    Value1,
    TextValue
}

public class DataClass {
    public Dictionary<MyEnum, string> Dict { get; set; }
}

public class BasicJsonTests {
    static readonly DataClass Decoded = new() {
        Dict = new Dictionary<MyEnum, string> {
            { MyEnum.Value1, "a" },
            { MyEnum.TextValue, "b" }
        }
    };

    const string Encoded = "{\"dict\":{\"value1\":\"a\",\"text_value\":\"b\"}}";

    [Fact]
    public void EnumDictionaryKeySerializeTest() {
        // Expected to fail.
        var v = JsonConvert.SerializeObject(Decoded, KifaJsonSerializerSettings.Default);
        Assert.Equal(Encoded, v);
    }

    [Fact]
    public void EnumDictionaryKeyDeserializeTest() {
        var v = JsonConvert.DeserializeObject<DataClass>(Encoded, KifaJsonSerializerSettings.Default);
        Assert.Equal(2, v.Dict.Count);
    }
}
