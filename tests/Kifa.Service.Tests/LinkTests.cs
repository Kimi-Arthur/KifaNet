using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Service.Tests;

public class LinkTests {
    string serialized =
        "{\"a_dict\":{\"very\":\"good\"},\"a_item\":\"15\",\"a_list\":[\"10\",\"12\"],\"id\":\"id\"}";

    DataB data = new DataB {
        Id = "id",
        AList = new List<Link<DataA>> {
            new(new DataA {
                Id = "10",
                MyValue = "b"
            }),
            new("12")
        },
        AItem = "15",
        ADict = new Dictionary<string, Link<DataA>> {
            { "very", new Link<DataA>("good") }
        }
    };

    [Fact]
    public void LinkSerializationTest() {
        Assert.Equal(serialized,
            JsonConvert.SerializeObject(data, Defaults.JsonSerializerSettings));
    }

    [Fact]
    public void LinkDeserializationTest() {
        Assert.Equal(data,
            JsonConvert.DeserializeObject<DataB>(serialized, Defaults.JsonSerializerSettings));
    }

    [Fact]
    public void LinkCloneTest() {
        Assert.Equal(data, data.Clone());
    }
}

class DataA : DataModel {
    public string? MyValue { get; set; }
}

class DataB : DataModel {
    public List<Link<DataA>>? AList { get; set; }
    public Dictionary<string, Link<DataA>> ADict { get; set; }
    public Link<DataA>? AItem { get; set; }
}
