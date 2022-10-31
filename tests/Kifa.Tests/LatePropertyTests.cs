using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Tests;

public class JsonType : JsonSerializable {
    public string Value { get; set; }

    public JsonType(string value) {
        Value = value;
    }

    public string ToJson() => Value;
}

public enum EnumType {
    Default,
    ValueA,
    ValueB,
    ValueC
}

public class LateClass {
    #region public late EnumType LateEnumProperty { get; set; }

    EnumType? lateEnumProperty;

    public EnumType LateEnumProperty {
        get => Late.Get(lateEnumProperty);
        set => Late.Set(ref lateEnumProperty, value);
    }

    #endregion

    #region public late string LateStringProperty { get; set; }

    string? lateStringProperty;

    public string LateStringProperty {
        get => Late.Get(lateStringProperty);
        set => Late.Set(ref lateStringProperty, value);
    }

    #endregion

    #region public late JsonType LateJsonProperty { get; set; }

    JsonType? lateJsonProperty;

    public JsonType LateJsonProperty {
        get => Late.Get(lateJsonProperty);
        set => Late.Set(ref lateJsonProperty, value);
    }

    #endregion

    public EnumType EnumProperty { get; set; }

    public JsonType? JsonProperty { get; set; }
}

public class LatePropertyTests {
    public static IEnumerable<object[]> PassingData
        => new List<object[]> {
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.ValueA,
                    LateEnumProperty = EnumType.ValueA,
                    LateJsonProperty = new JsonType("bcd"),
                    LateStringProperty = "abc",
                    JsonProperty = new JsonType("hid")
                },
                "{\"enum_property\":\"value_a\",\"json_property\":\"hid\"," +
                "\"late_enum_property\":\"value_a\",\"late_json_property\":\"bcd\"," +
                "\"late_string_property\":\"abc\"}"
            },
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.Default,
                    LateJsonProperty = new JsonType(""),
                    LateStringProperty = "",
                    LateEnumProperty = EnumType.Default
                },
                "{\"late_json_property\":\"\",\"late_string_property\":\"\"}"
            }
        };

    [Theory]
    [MemberData(nameof(PassingData))]
    public void LatePropertyCloneTest(LateClass data, string serialized) {
        var copy = data.Clone();

        Assert.Equal(serialized,
            JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings));
        Assert.Equal(serialized,
            JsonConvert.SerializeObject(copy, Defaults.JsonSerializerSettings));
        Assert.Equal(JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings),
            JsonConvert.SerializeObject(copy, Defaults.JsonSerializerSettings));
    }

    public static IEnumerable<object[]> FailingData
        => new List<object[]> {
            new object[] { new LateClass() },
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.Default,
                    LateEnumProperty = EnumType.Default,
                    // LateJsonProperty = new JsonType(""),
                    JsonProperty = new JsonType(""),
                    LateStringProperty = ""
                }
            },
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.Default,
                    // LateEnumProperty = EnumType.Default,
                    LateJsonProperty = new JsonType(""),
                    JsonProperty = new JsonType(""),
                    LateStringProperty = ""
                }
            },
            new object[] {
                new LateClass {
                    EnumProperty = EnumType.Default,
                    LateEnumProperty = EnumType.Default,
                    LateJsonProperty = new JsonType(""),
                    JsonProperty = new JsonType(""),
                    // LateStringProperty = ""
                }
            }
        };

    [Theory]
    [MemberData(nameof(FailingData))]
    public void LatePropertyCloneExceptionTest(LateClass data) {
        var exception = Assert.Throws<JsonSerializationException>(() => data.Clone());
        Assert.IsType<NullReferenceException>(exception.InnerException);
    }
}
