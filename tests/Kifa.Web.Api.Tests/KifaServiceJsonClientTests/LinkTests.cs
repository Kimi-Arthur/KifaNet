using System;
using System.Collections.Generic;
using System.IO;
using Kifa.Service;
using Xunit;

namespace Kifa.Web.Api.Tests; 

public class TestDataModelWithVirtualLinks : DataModel<TestDataModelWithVirtualLinks> {
    public const string ModelId = "link_tests";

    public string? Data { get; set; }

    public override SortedSet<string> GetVirtualItems() =>
        Data == null
            ? new SortedSet<string>()
            : new SortedSet<string> {
                Data
            };
}

public class LinkTests {
    readonly string folder = Path.GetTempPath() + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

    public LinkTests() {
        KifaServiceJsonClient.DataFolder = folder;
    }

    [Fact]
    public void SetGetWithVirtualLinksTest() {
        KifaServiceJsonClient<TestDataModelWithVirtualLinks> client = new();

        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test",
            Data = "very good data"
        });

        var data = client.Get("test");
        Assert.Equal("test", data.Id);
        Assert.Equal("very good data", data.Data);

        var linkedData = client.Get("very good data");
        Assert.Equal("very good data", linkedData.Id);
        Assert.Equal("very good data", linkedData.Data);
    }

    public void Dispose() {
        Directory.Delete(folder, true);
    }}
