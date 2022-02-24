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

public class JsonTests {
    static readonly DataClass Decoded = new DataClass {
        Dict = new Dictionary<MyEnum, string> {{MyEnum.Value1, "a"}, {MyEnum.TextValue, "b"}}
    };

    const string Encoded = "{\"dict\":{\"value1\":\"a\",\"text_value\":\"b\"}}";

    [Fact]
    public void EnumDictionaryKeySerializeTest() {
        var v = JsonConvert.SerializeObject(Decoded, Defaults.JsonSerializerSettings);
        Assert.Equal(Encoded, v);
    }

    [Fact]
    public void EnumDictionaryKeyDeserializeTest() {
        var v = JsonConvert.DeserializeObject<DataClass>(Encoded, Defaults.JsonSerializerSettings);
        Assert.Equal(2, v.Dict.Count);
    }
}