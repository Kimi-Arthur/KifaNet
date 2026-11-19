using System;
using System.Collections.Generic;
using Xunit;

namespace Kifa.Service.Tests;

public class LinkTests {
    string serializedExpanded =
        "{\"id\":\"id\",\"a_list\":[{\"id\":\"10\",\"my_value\":\"b\"},\"12\"],\"a_dict\":{\"very\":\"good\"},\"a_item\":\"15\"}";

    string serializedIdsOnly =
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
        // Without Expand(), ToJson() serializes only IDs even when Data is populated in memory.
        Assert.Equal(serializedIdsOnly, data.ToJson());

        data.AList![0].Expand();
        Assert.Equal(serializedExpanded, data.ToJson());
        Assert.Equal(serializedIdsOnly, data.ToDataJson());
    }

    [Fact]
    public void LinkDeserializationTest() {
        var fromExpanded = serializedExpanded.FromJson<DataB>();
        Assert.Equal(data, fromExpanded);
        Assert.Equal("b", fromExpanded!.AList![0].Data?.MyValue);

        var fromIdsOnly = serializedIdsOnly.FromJson<DataB>();
        Assert.Equal(data, fromIdsOnly);
    }

    [Fact]
    public void LinkCloneTest() {
        data.AList![0].Expand();
        var cloned = data.Clone();
        Assert.Equal(data, cloned);
        Assert.Equal("b", cloned.AList![0].Data?.MyValue);
    }

    [Fact]
    public void LinkSharedReferenceSerializationTest() {
        var sharedA = new DataA {
            Id = "shared",
            MyValue = "val"
        };
        var dag = new DataB {
            Id = "dag",
            AList = [sharedA, sharedA]
        };
        dag.AList[0].Expand();
        dag.AList[1].Expand();
        Assert.Equal(
            "{\"id\":\"dag\",\"a_list\":[{\"id\":\"shared\",\"my_value\":\"val\"},{\"id\":\"shared\",\"my_value\":\"val\"}]}",
            dag.ToJson());
    }
}

class DataA : DataModel, WithModelId<DataA> {
    public static string ModelId => "as";

    public override TimeSpan? RefreshInterval => RefreshIntervals.None;

    public string? MyValue { get; set; }
}

class DataB : DataModel, WithModelId<DataB> {
    public static string ModelId => "bs";

    public override TimeSpan? RefreshInterval => RefreshIntervals.None;

    public List<Link<DataA>>? AList { get; set; }
    public Dictionary<string, Link<DataA>> ADict { get; set; }
    public Link<DataA>? AItem { get; set; }
    public Link<DataB>? BItem { get; set; }
}
