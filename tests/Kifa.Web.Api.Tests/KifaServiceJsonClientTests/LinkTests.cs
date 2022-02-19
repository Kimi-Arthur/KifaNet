using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
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
                VirtualItemPrefix + Data
            };
}

public class LinkTests : IDisposable {
    readonly string folder = Path.GetTempPath() + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    readonly KifaServiceJsonClient<TestDataModelWithVirtualLinks> client = new();

    public LinkTests() {
        KifaServiceJsonClient.DataFolder = folder;
    }

    [Fact]
    public void GetTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test",
            Data = "very good data"
        });

        var data = client.Get("test");
        data.Metadata.Linking.Target.Should().BeNull();
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/very good data");
        data.Id.Should().Be("test");
        data.Data.Should().Be("very good data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Metadata.Linking.Target.Should().Be("test");
        linkedData.Id.Should().Be("/$/very good data");
        linkedData.Data.Should().Be("very good data");
    }

    [Fact]
    public void DeleteTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test",
            Data = "very good data"
        });

        client.Delete("test");

        var data = client.Get("test");
        data.Should().BeNull();

        var linkedData = client.Get("/$/very good data");
        linkedData.Should().BeNull();
    }

    [Fact]
    public void DeleteVirtualTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test",
            Data = "very good data"
        });

        var actionResult = client.Delete("/$/very good data");
        actionResult.Status.Should().Be(KifaActionStatus.BadRequest);

        var data = client.Get("test");
        data.Id.Should().NotBeNull();

        var linkedData = client.Get("/$/very good data");
        linkedData.Id.Should().NotBeNull();
    }

    [Fact]
    public void LinkTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");

        var data = client.Get("new_test");

        data.Id.Should().Be("new_test");
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Links.Should().HaveCount(1).And.Contain("new_test");
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/very good data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Metadata.Linking.Target.Should().Be("test");
    }

    [Fact]
    public void DeleteTargetTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");
        client.Delete("test");

        client.Get("test").Should().BeNull();

        var data = client.Get("new_test");

        data.Id.Should().Be("new_test");
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Target.Should().BeNull();
        data.Metadata.Linking.Links.Should().BeNull();
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/very good data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Metadata.Linking.Target.Should().Be("new_test");
    }

    [Fact]
    public void DeleteLinkTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "new_test");
        client.Delete("new_test");

        client.Get("new_test").Should().BeNull();

        var data = client.Get("test");

        data.Id.Should().Be("test");
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Target.Should().BeNull();
        data.Metadata.Linking.Links.Should().BeNull();
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/very good data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Metadata.Linking.Target.Should().Be("test");
    }

    public void Dispose() {
        Directory.Delete(folder, true);
    }
}
