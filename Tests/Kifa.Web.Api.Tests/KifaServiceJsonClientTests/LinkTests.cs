using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kifa.Service;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class TestDataModelWithVirtualLinks : DataModel, WithModelId<TestDataModelWithVirtualLinks> {
    public static string ModelId => "link_tests";

    public string? Data { get; set; }

    public Dictionary<string, string>? ExtraDict { get; set; }

    public string? OtherProp { get; set; }

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

        client.DataFolder = folder;
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

    [Fact]
    public void FixVirtualLinksTest() {
        var id = nameof(FixVirtualLinksTest);
        var rawPath = $"{folder}/link_tests/{id}.json";
        Directory.CreateDirectory(Path.GetDirectoryName(rawPath)!);
        File.WriteAllText(rawPath, new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "raw data"
        }.ToJson());

        client.Get("/$/raw data").Should().BeNull();
        var rawInitial = client.Get(id);
        rawInitial.Metadata?.Linking?.VirtualLinks.Should().BeNull();

        var result = client.FixVirtualLinks(new FixOptions());
        result.Status.Should().Be(KifaActionStatus.OK);

        var virtualItem = client.Get("/$/raw data");
        virtualItem.Should().NotBeNull();
        virtualItem!.Metadata.Linking.Target.Should().Be(id);

        var updatedItem = client.Get(id);
        updatedItem!.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/raw data");
    }

    [Fact]
    public void FixVirtualLinksRecreatesMissingVirtualLinkTest() {
        var id = nameof(FixVirtualLinksRecreatesMissingVirtualLinkTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "raw data"
        });

        var virtualPath = $"{folder}/link_tests/$/raw data.json";
        File.Exists(virtualPath).Should().BeTrue();
        File.Delete(virtualPath);
        File.Exists(virtualPath).Should().BeFalse();

        var result = client.FixVirtualLinks(new FixOptions());
        result.Status.Should().Be(KifaActionStatus.OK);

        File.Exists(virtualPath).Should().BeTrue();
        var virtualItem = client.Get("/$/raw data");
        virtualItem.Should().NotBeNull();
        virtualItem!.Metadata.Linking.Target.Should().Be(id);
    }

    [Fact]
    public void FixVirtualLinksDetectsCollisionAndFailsOnConflictingPropertiesTest() {
        var id1 = $"{nameof(FixVirtualLinksDetectsCollisionAndFailsOnConflictingPropertiesTest)}_1";
        var id2 = $"{nameof(FixVirtualLinksDetectsCollisionAndFailsOnConflictingPropertiesTest)}_2";

        // Item 1 set first, creates virtual link
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id1,
            Data = "shared data",
            OtherProp = "val1"
        });

        // Item 2 written directly with virtual link metadata to simulate out-of-band / existing data
        var rawPath = $"{folder}/link_tests/{id2}.json";
        File.WriteAllText(rawPath, new TestDataModelWithVirtualLinks {
            Id = id2,
            Data = "shared data",
            OtherProp = "val2",
            Metadata = new DataMetadata {
                Linking = new LinkingMetadata {
                    VirtualLinks = ["/$/shared data"]
                }
            }
        }.ToJson());

        var result = client.FixVirtualLinks(new FixOptions());
        result.Status.Should().Be(KifaActionStatus.Error);

        var batchResult = result as KifaBatchActionResult;
        batchResult.Should().NotBeNull();
        batchResult!.Results.First(r => r.Item == id1).Result.Status.Should()
            .Be(KifaActionStatus.OK);
        var item2Result = batchResult.Results.First(r => r.Item == id2).Result;
        item2Result.Status.Should().Be(KifaActionStatus.Error);
        item2Result.Message.Should().Contain("conflicting values for OtherProp");
    }

    [Fact]
    public void FixVirtualLinksMergesItemsSuccessfullyWhenFieldsToMergeMatchesTest() {
        var id1 = $"{nameof(FixVirtualLinksMergesItemsSuccessfullyWhenFieldsToMergeMatchesTest)}_1";
        var id2 = $"{nameof(FixVirtualLinksMergesItemsSuccessfullyWhenFieldsToMergeMatchesTest)}_2";

        client.Set(new TestDataModelWithVirtualLinks {
            Id = id1,
            Data = "shared data",
            ExtraDict = new Dictionary<string, string> {
                { "key1", "val1" }
            }
        });

        var rawPath = $"{folder}/link_tests/{id2}.json";
        File.WriteAllText(rawPath, new TestDataModelWithVirtualLinks {
            Id = id2,
            Data = "shared data",
            ExtraDict = new Dictionary<string, string> {
                { "key2", "val2" }
            },
            Metadata = new DataMetadata {
                Linking = new LinkingMetadata {
                    VirtualLinks = ["/$/shared data"]
                }
            }
        }.ToJson());

        var result = client.FixVirtualLinks(new FixOptions {
            FieldsToMerge = ["ExtraDict"]
        });
        result.Status.Should().Be(KifaActionStatus.OK);

        var item1 = client.Get(id1);
        item1.Should().NotBeNull();
        item1!.ExtraDict.Should().ContainKey("key1").WhoseValue.Should().Be("val1");
        item1.ExtraDict.Should().ContainKey("key2").WhoseValue.Should().Be("val2");
        item1.Metadata.Linking.Links.Should().Contain(id2);

        var item2 = client.Get(id2);
        item2.Should().NotBeNull();
        item2!.Metadata.Linking.Target.Should().Be(id1);
    }

    [Fact]
    public void ConcurrentVirtualLinkWriteTest() {
        var items = Enumerable.Range(0, 10).Select(i => new TestDataModelWithVirtualLinks {
            Id = $"item_{i}",
            Data = $"shared_virtual_item_{i % 2}"
        }).ToList();

        var successCount = 0;
        var conflictCount = 0;

        Parallel.ForEach(items, item => {
            var res = client.Set(item);
            if (res.Status == KifaActionStatus.OK) {
                Interlocked.Increment(ref successCount);
            } else if (res.Message?.Contains(nameof(VirtualItemAlreadyLinkedException)) == true) {
                Interlocked.Increment(ref conflictCount);
            }
        });

        // Exactly 2 items (one for shared_virtual_item_0, one for shared_virtual_item_1) should succeed
        // and 8 items should fail cleanly with VirtualItemAlreadyLinkedException due to locking.
        successCount.Should().Be(2);
        conflictCount.Should().Be(8);
    }

    public void Dispose() {
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
