using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Tests;

public class JsonType : JsonSerializable {
    public required string Value { get; set; }

    public static implicit operator JsonType(string value)
        => new() {
            Value = value
        };

    public string ToJson() => Value;
}

public enum EnumType {
    Default,
    ValueA,
    ValueB,
    ValueC
}

public class LateClass {
    public EnumType EnumProperty { get; set; }

    public EnumType LateEnumProperty {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public JsonType LateJsonProperty {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public string LateStringProperty {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public JsonType? JsonProperty { get; set; }

    public string? NullableStringProperty { get; set; }

    public string StringProperty { get; set; } = "";

    public List<string> ListProperty { get; set; } = new();

    public Dictionary<string, string> DictProperty { get; set; } = new();
}

public class LatePropertyTests {
    public static IEnumerable<object[]> PassingData
        => new List<object[]> {
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.ValueA,
                    LateEnumProperty = EnumType.ValueA,
                    LateJsonProperty = "bcd",
                    LateStringProperty = "abc",
                    JsonProperty = "hid",
                    NullableStringProperty = "nullok",
                    StringProperty = "ok",
                    ListProperty = new List<string> {
                        "a",
                        "d",
                        "f"
                    },
                    DictProperty = new Dictionary<string, string> {
                        { "Good", "Bad" }
                    }
                },
                "{\"enum_property\":\"value_a\",\"late_enum_property\":\"value_a\",\"late_json_property\":\"bcd\",\"late_string_property\":\"abc\",\"json_property\":\"hid\",\"nullable_string_property\":\"nullok\",\"string_property\":\"ok\",\"list_property\":[\"a\",\"d\",\"f\"],\"dict_property\":{\"Good\":\"Bad\"}}"
            },
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.Default,
                    LateJsonProperty = "",
                    LateStringProperty = "",
                    LateEnumProperty = EnumType.Default,
                    JsonProperty = "",
                    NullableStringProperty = ""
                },
                "{\"late_json_property\":\"\",\"json_property\":\"\",\"nullable_string_property\":\"\"}"
            }
        };

    [Theory]
    [MemberData(nameof(PassingData))]
    public void LatePropertyRoundTripTest(LateClass data, string serialized) {
        var copy = data.Clone();

        Assert.Equal(serialized,
            JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default));
        Assert.Equal(serialized,
            JsonConvert.SerializeObject(copy, KifaJsonSerializerSettings.Default));
        Assert.Equal(JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default),
            JsonConvert.SerializeObject(copy, KifaJsonSerializerSettings.Default));
    }

    public static IEnumerable<object[]> FailingData
        => new List<object[]> {
            new object[] { new LateClass() },
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.Default,
                    LateEnumProperty = EnumType.Default,
                    // LateJsonProperty = "",
                    JsonProperty = "",
                    LateStringProperty = ""
                }
            },
            // new object[] {
            //     new LateClass {
            //         EnumProperty = EnumType.Default,
            //         // LateEnumProperty = EnumType.Default,
            //         LateJsonProperty = "",
            //         JsonProperty = "",
            //         LateStringProperty = ""
            //     }
            // },
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.Default,
                    LateEnumProperty = EnumType.Default,
                    LateJsonProperty = "",
                    JsonProperty = "",
                    // LateStringProperty = ""
                }
            }
        };

    [Theory]
    [MemberData(nameof(FailingData))]
    public void
        LatePropertyWithMissingFieldsShouldThrowWhenSerializingExceptionTest(LateClass data) {
        var exception = Assert.Throws<JsonSerializationException>(()
            => JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default));
        Assert.IsType<NullReferenceException>(exception.InnerException);
    }

    [Fact]
    public void LatePropertyDeserializeIsToDefaultTest() {
        var data = JsonConvert.DeserializeObject<LateClass>("{}");
        Assert.Empty(data!.ListProperty);
        Assert.Empty(data.DictProperty);

        Assert.Throws<NullReferenceException>(() => data.LateStringProperty);
        Assert.Throws<JsonSerializationException>(()
            => JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default));
        data.LateStringProperty = "";
        Assert.Equal("", data.LateStringProperty);
        data.LateJsonProperty = "";
        data.LateEnumProperty = EnumType.Default;
        Assert.Equal("{\"late_json_property\":\"\"}",
            JsonConvert.SerializeObject(data, KifaJsonSerializerSettings.Default));
    }
}
