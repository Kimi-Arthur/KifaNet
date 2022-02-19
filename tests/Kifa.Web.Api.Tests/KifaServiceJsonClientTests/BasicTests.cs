using System;
using System.IO;
using FluentAssertions;
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
    readonly KifaServiceJsonClient<TestDataModel> client = new();

    public BasicTests() {
        KifaServiceJsonClient.DataFolder = folder;
    }

    [Fact]
    public void GetTest() {
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
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        var data = client.Get("test");
        Assert.Equal("test", data.Id);
        Assert.Equal("very good data", data.Data);
    }

    [Fact]
    public void DeleteTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Delete("test");

        var data = client.Get("test");
        data.Should().BeNull();
    }

    [Fact]
    public void DeleteNonExistTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        var result = client.Delete("test1");

        result.Status.Should().Be(KifaActionStatus.BadRequest);

        var data = client.Get("test");
        Assert.Equal("test", data.Id);
        Assert.Equal("very good data", data.Data);
    }

    public void Dispose() {
        Directory.Delete(folder, true);
    }
}
