using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Service.Tests;

public class LinkTests {
    [Fact]
    public void LinkSerializationTest() {
        var b = new DataB {
            Id = "id",
            AList = new List<Link<DataA>> {
                new(new DataA {
                    Id = "10",
                    MyValue = "b"
                }),
                new("12")
            },
            AItem = "15"
        };

        b.ADict = new Dictionary<string, Link<DataA>>();
        b.ADict["very"] = new Link<DataA>("good");

        Assert.Equal(
            "{\"a_dict\":{\"very\":\"good\"},\"a_item\":\"15\",\"a_list\":[\"10\",\"12\"],\"id\":\"id\"}",
            JsonConvert.SerializeObject(b, Defaults.JsonSerializerSettings));
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
