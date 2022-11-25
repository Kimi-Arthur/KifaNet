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

    public override SortedSet<string> GetVirtualItems()
        => Data == null
            ? new SortedSet<string>()
            : new SortedSet<string> {
                VirtualItemPrefix + Data
            };
}

public class LinkTests : IDisposable {
    readonly string folder;
    readonly KifaServiceJsonClient<TestDataModelWithVirtualLinks> client = new();

    public LinkTests() {
        folder = $"{Path.GetTempPath()}/{nameof(LinkTest)}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        KifaServiceJsonClient.DataFolder = folder;
    }

    [Fact]
    public void GetTest() {
        var id = nameof(GetTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        var data = client.Get(id);
        data.Metadata.Linking.Target.Should().BeNull();
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/very good data");
        data.Id.Should().Be(id);
        data.Data.Should().Be("very good data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Metadata.Linking.Target.Should().Be(id);
        linkedData.Id.Should().Be("/$/very good data");
        linkedData.Data.Should().Be("very good data");
    }

    [Fact]
    public void LinkTest() {
        var id = nameof(LinkTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        client.Link(id, "new_test");

        var data = client.Get("new_test");

        data.Id.Should().Be("new_test");
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Links.Should().HaveCount(1).And.Contain("new_test");
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/very good data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Metadata.Linking.Target.Should().Be(id);
    }

    [Fact]
    public void DeleteTest() {
        var id = nameof(DeleteTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        client.Delete(id);

        var data = client.Get(id);
        data.Should().BeNull();

        var linkedData = client.Get("/$/very good data");
        linkedData.Should().BeNull();
    }

    [Fact]
    public void DeleteVirtualTest() {
        var id = nameof(DeleteVirtualTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        var actionResult = client.Delete("/$/very good data");
        actionResult.Status.Should().Be(KifaActionStatus.BadRequest);

        var data = client.Get(id);
        data.Id.Should().NotBeNull();

        var linkedData = client.Get("/$/very good data");
        linkedData.Id.Should().NotBeNull();
    }

    [Fact]
    public void DeleteTargetTest() {
        var id = nameof(DeleteTargetTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        client.Link(id, "new_test");
        client.Delete(id);

        client.Get(id).Should().BeNull();

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
        var id = nameof(DeleteLinkTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        client.Link(id, "new_test");
        client.Delete("new_test");

        client.Get("new_test").Should().BeNull();

        var data = client.Get(id);

        data.Id.Should().Be(id);
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Target.Should().BeNull();
        data.Metadata.Linking.Links.Should().BeNull();
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/very good data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Metadata.Linking.Target.Should().Be(id);
    }

    [Fact]
    public void VirtualItemDisappearTest() {
        var id = nameof(VirtualItemDisappearTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        client.Set(new TestDataModelWithVirtualLinks {
            Id = id
        });

        var data = client.Get(id);
        data.Metadata?.Linking.Should().BeNull();

        var linkedData = client.Get("/$/very good data");
        linkedData.Should().BeNull();
    }

    [Fact]
    public void VirtualItemUpdateTest() {
        var id = nameof(VirtualItemUpdateTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        client.Update(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "ok data"
        });

        var data = client.Get(id);
        data.Metadata.Linking.Target.Should().BeNull();
        data.Metadata.Linking.Links.Should().BeNull();
        data.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/ok data");

        var linkedData = client.Get("/$/very good data");
        linkedData.Should().BeNull();

        linkedData = client.Get("/$/ok data");
        linkedData.Metadata.Linking.Target.Should().Be(id);
        linkedData.Id.Should().Be("/$/ok data");
        linkedData.Data.Should().Be("ok data");
    }

    [Fact]
    public void ListTest() {
        var id = nameof(ListTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        client.Set(new TestDataModelWithVirtualLinks {
            Id = "test1",
            Data = "ok data"
        });

        client.Link(id, "new_test");

        var items = client.List();
        items.Should().HaveCount(3).And.Contain(
            new KeyValuePair<string, TestDataModelWithVirtualLinks>[] {
                new(id, new TestDataModelWithVirtualLinks {
                    Id = id,
                    Data = "very good data",
                    Metadata = new DataMetadata {
                        Linking = new LinkingMetadata {
                            Links = new SortedSet<string> {
                                "new_test"
                            },
                            VirtualLinks = new SortedSet<string> {
                                "/$/very good data"
                            }
                        }
                    }
                }),
                new("new_test", new TestDataModelWithVirtualLinks {
                    Id = "new_test",
                    Data = "very good data",
                    Metadata = new DataMetadata {
                        Linking = new LinkingMetadata {
                            Target = id,
                            Links = new SortedSet<string> {
                                "new_test"
                            },
                            VirtualLinks = new SortedSet<string> {
                                "/$/very good data"
                            }
                        }
                    }
                }),
                new("test1", new TestDataModelWithVirtualLinks {
                    Id = "test1",
                    Data = "ok data",
                    Metadata = new DataMetadata {
                        Linking = new LinkingMetadata {
                            VirtualLinks = new SortedSet<string> {
                                "/$/ok data"
                            }
                        }
                    }
                })
            });
    }

    public void Dispose() {
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
