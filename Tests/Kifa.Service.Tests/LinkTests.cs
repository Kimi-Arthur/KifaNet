using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Service.Tests;

public class LinkTests {
    string serialized =
        "{\"id\":\"id\",\"a_list\":[\"10\",\"12\"],\"a_dict\":{\"very\":\"good\"},\"a_item\":\"15\"}";

    DataB data = new DataB {
        Id = "id",
        AList = new List<Link<DataA>> {
            new DataA {
                Id = "10",
                MyValue = "b"
            },
            "12"
        },
        AItem = "15",
        ADict = new Dictionary<string, Link<DataA>> {
            { "very", "good" }
        }
    };

    [Fact]
    public void LinkSerializationTest() {
        Assert.Equal(serialized, data.ToJson());
    }

    [Fact]
    public void LinkDeserializationTest() {
        Assert.Equal(data, serialized.FromJson<DataB>());
    }

    [Fact]
    public void LinkCloneTest() {
        Assert.Equal(data, data.Clone());
    }
}

class DataA : DataModel, WithModelId<DataA> {
    public static string ModelId => "as";

    public string? MyValue { get; set; }
}

class DataB : DataModel, WithModelId<DataB> {
    public static string ModelId => "bs";

    public List<Link<DataA>>? AList { get; set; }
    public Dictionary<string, Link<DataA>> ADict { get; set; }
    public Link<DataA>? AItem { get; set; }
}
