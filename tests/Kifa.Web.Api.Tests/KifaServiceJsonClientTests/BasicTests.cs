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
    public void LinkTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");

        var data = client.Get("new_test");

        data.Id.Should().Be("new_test");
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Links.Should().HaveCount(1).And.Contain("new_test");
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

    [Fact]
    public void DeleteTargetTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");
        client.Delete("test");

        client.Get("test").Should().BeNull();

        var data = client.Get("new_test");

        data.Id.Should().Be("new_test");
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Should().BeNull();
    }

    [Fact]
    public void DeleteLinkTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");
        client.Delete("new_test");

        client.Get("new_test").Should().BeNull();

        var data = client.Get("test");

        data.Id.Should().Be("test");
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Should().BeNull();
    }

    [Fact]
    public void UpdateTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Update(new TestDataModel {
            Id = "test",
            Data = "ok data"
        });

        var data = client.Get("test");
        data.Id.Should().Be("test");
        data.Data.Should().Be("ok data");
    }

    [Fact]
    public void UpdateViaTargetTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");

        client.Update(new TestDataModel {
            Id = "test",
            Data = "ok data"
        });

        var data = client.Get("new_test");
        data.Id.Should().Be("new_test");
        data.Data.Should().Be("ok data");
    }

    [Fact]
    public void UpdateViaLinkTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");

        client.Update(new TestDataModel {
            Id = "new_test",
            Data = "ok data"
        });

        var data = client.Get("test");
        data.Id.Should().Be("test");
        data.Data.Should().Be("ok data");

        var linkedData = client.Get("new_test");
        linkedData.Id.Should().Be("new_test");
        linkedData.Data.Should().Be("ok data");
    }

    public void Dispose() {
        Directory.Delete(folder, true);
    }
}
