using System;
using System.IO;
using Kifa.Service;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class TestDataModel : DataModel<TestDataModel> {
    public const string ModelId = "tests";

    public string? Data { get; set; }
}

public class BasicTests : IDisposable {
    readonly string folder = Path.GetTempPath() + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

    public BasicTests() {
        KifaServiceJsonClient.DataFolder = folder;
    }

    [Fact]
    public void GetTest() {
        KifaServiceJsonClient<TestDataModel> client = new();
        Directory.CreateDirectory(folder + "/tests");
        File.WriteAllText(folder + "/tests/test.json", JsonConvert.SerializeObject(new TestDataModel {
            Id = "test",
            Data = "good data"
        }, Defaults.PrettyJsonSerializerSettings));

        var data = client.Get("test");

        Assert.Equal("test", data.Id);
        Assert.Equal("good data", data.Data);

        var nullData = client.Get("not found");
        Assert.Null(nullData);
    }

    [Fact]
    public void SetGetTest() {
        KifaServiceJsonClient<TestDataModel> client = new();

        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        var data = client.Get("test");
        Assert.Equal("test", data.Id);
        Assert.Equal("very good data", data.Data);
    }

    public void Dispose() {
        Directory.Delete(folder, true);
    }
}
