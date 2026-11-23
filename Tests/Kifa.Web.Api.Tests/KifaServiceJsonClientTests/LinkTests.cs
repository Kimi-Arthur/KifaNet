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

    public static KifaServiceClient<TestDataModelWithVirtualLinks> Client { get; set; } =
        new KifaServiceRestClient<TestDataModelWithVirtualLinks>();

    public static Dictionary<string, string> Redirects { get; } = new();
    public static Dictionary<string, string> FillableData { get; } = new();

    public string? Data { get; set; }

    public Dictionary<string, string>? ExtraDict { get; set; }

    public string? OtherProp { get; set; }

    public List<Link<TestDataModelWithVirtualLinks>>? Peers { get; set; }

    public List<TestSubContainer>? SubItems { get; set; }

    public class TestSubContainer {
        public Link<TestDataModelWithVirtualLinks>? Peer { get; set; }
    }

    public override TimeSpan? RefreshInterval => RefreshIntervals.Infinity;

    public override void Fill(FillContext context) {
        if (RealId != null && Redirects.TryGetValue(RealId, out var targetId)) {
            throw new DataIsLinkedException(targetId);
        }

        if (RealId != null && FillableData.TryGetValue(RealId, out var val)) {
            Data = val;
            return;
        }

        throw new NoNeedToFillException();
    }

    public override SortedSet<string> GetVirtualLinks()
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
        folder =
            $"{Path.GetTempPath()}/{nameof(LinkTest)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}";

        client.DataFolder = folder;
        TestDataModelWithVirtualLinks.Client = client;
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
        actionResult.Message.Should().Contain(id);

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
    public void FixLinksTest() {
        var id = nameof(FixLinksTest);
        var rawPath = $"{folder}/link_tests/{id}.json";
        Directory.CreateDirectory(Path.GetDirectoryName(rawPath)!);
        File.WriteAllText(rawPath, new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "raw data"
        }.ToJson());

        client.Get("/$/raw data").Should().BeNull();
        var rawInitial = client.Get(id);
        rawInitial.Metadata?.Linking?.VirtualLinks.Should().BeNull();

        var result = client.FixLinks(new FixOptions());
        result.Status.Should().Be(KifaActionStatus.OK);

        var virtualItem = client.Get("/$/raw data");
        virtualItem.Should().NotBeNull();
        virtualItem!.Metadata.Linking.Target.Should().Be(id);

        var updatedItem = client.Get(id);
        updatedItem!.Metadata.Linking.VirtualLinks.Should().HaveCount(1).And.Contain("/$/raw data");
    }

    [Fact]
    public void FixLinksRecreatesMissingVirtualLinkTest() {
        var id = nameof(FixLinksRecreatesMissingVirtualLinkTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "raw data"
        });

        var virtualPath = $"{folder}/link_tests/$/raw data.json";
        File.Exists(virtualPath).Should().BeTrue();
        File.Delete(virtualPath);
        File.Exists(virtualPath).Should().BeFalse();

        var result = client.FixLinks(new FixOptions());
        result.Status.Should().Be(KifaActionStatus.OK);

        File.Exists(virtualPath).Should().BeTrue();
        var virtualItem = client.Get("/$/raw data");
        virtualItem.Should().NotBeNull();
        virtualItem!.Metadata.Linking.Target.Should().Be(id);
    }

    [Fact]
    public void FixLinksDetectsCollisionAndFailsOnConflictingPropertiesTest() {
        var id1 = $"{nameof(FixLinksDetectsCollisionAndFailsOnConflictingPropertiesTest)}_1";
        var id2 = $"{nameof(FixLinksDetectsCollisionAndFailsOnConflictingPropertiesTest)}_2";

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

        var result = client.FixLinks(new FixOptions());
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
    public void FixLinksMergesItemsSuccessfullyWhenFieldsToMergeMatchesTest() {
        var id1 = $"{nameof(FixLinksMergesItemsSuccessfullyWhenFieldsToMergeMatchesTest)}_1";
        var id2 = $"{nameof(FixLinksMergesItemsSuccessfullyWhenFieldsToMergeMatchesTest)}_2";

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

        var result = client.FixLinks(new FixOptions {
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
            } else if (res.Message?.Contains(nameof(VirtualLinkAlreadyExistsException)) == true) {
                Interlocked.Increment(ref conflictCount);
            }
        });

        // Exactly 2 items (one for shared_virtual_item_0, one for shared_virtual_item_1) should succeed
        // and 8 items should fail cleanly with VirtualLinkAlreadyExistsException due to locking.
        successCount.Should().Be(2);
        conflictCount.Should().Be(8);
    }

    [Fact]
    public void GetUnlistedLinkThrowsDataCorruptedExceptionTest() {
        var id = nameof(GetUnlistedLinkThrowsDataCorruptedExceptionTest);
        client.Set(new TestDataModelWithVirtualLinks {
            Id = id,
            Data = "very good data"
        });

        var rogueLinkId = "rogue_link";
        var roguePath = $"{folder}/link_tests/{rogueLinkId}.json";
        File.WriteAllText(roguePath, new TestDataModelWithVirtualLinks {
            Id = rogueLinkId,
            Metadata = new DataMetadata {
                Linking = new LinkingMetadata {
                    Target = id
                }
            }
        }.ToJson());

        var act = () => client.Get(rogueLinkId);
        act.Should().Throw<DataCorruptedException>()
            .WithMessage($"*Target {id} does not contain link {rogueLinkId}*");
    }

    [Fact]
    public void DataIsLinkedExceptionAutoFetchesTargetAndRejectsExistingInstanceTest() {
        TestDataModelWithVirtualLinks.Redirects.Clear();
        TestDataModelWithVirtualLinks.FillableData.Clear();
        try {
            TestDataModelWithVirtualLinks.Redirects["alias_id"] = "canonical_id";
            TestDataModelWithVirtualLinks.FillableData["canonical_id"] = "filled:canonical_id";

            var linked = client.Get("alias_id");
            linked.Should().NotBeNull();
            linked!.Id.Should().Be("alias_id");
            linked.RealId.Should().Be("canonical_id");
            linked.Data.Should().Be("filled:canonical_id");
            linked.Metadata!.Linking!.Links.Should().Contain("alias_id");

            // Existing standalone instance must not be overwritten if it later throws DataIsLinkedException
            client.Set(new TestDataModelWithVirtualLinks {
                Id = "existing_id",
                Data = "original_data"
            });
            TestDataModelWithVirtualLinks.Redirects["existing_id"] = "canonical_id";

            var refreshed = client.Get("existing_id", new KifaDataOptions {
                Refresh = true
            });
            refreshed.Should().NotBeNull();
            refreshed!.Id.Should().Be("existing_id");
            refreshed.RealId.Should().Be("existing_id");
            refreshed.Data.Should().Be("original_data");
        } finally {
            TestDataModelWithVirtualLinks.Redirects.Clear();
            TestDataModelWithVirtualLinks.FillableData.Clear();
        }
    }

    [Fact]
    public void CyclicLinkedFieldsTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "movie1",
            Data = "Movie 1",
            Peers = ["actor1"]
        });
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "actor1",
            Data = "Actor 1",
            Peers = ["movie1"]
        });

        // 0 hops: Peers serialized as string IDs
        var zeroHops = client.Get("movie1")!;
        zeroHops.ToJson().Should().Contain("\"peers\":[\"actor1\"]");

        // 1 hop ("Peers"): movie1 -> actor1 expanded, actor1.Peers remains string ID "movie1"
        var oneHop = client.Get("movie1", new KifaDataOptions {
            LinkedFields = ["Peers"]
        })!;
        oneHop.ToJson().Should().Contain("\"id\":\"actor1\"").And.Contain("\"peers\":[\"movie1\"]");

        // 2 hops ("Peers.Peers"): movie1 -> actor1 expanded -> movie1 expanded, inner movie1.Peers remains string ID "actor1"
        var twoHops = client.Get("movie1", new KifaDataOptions {
            LinkedFields = ["Peers.Peers"]
        })!;
        var json = twoHops.ToJson();
        json.Should().Contain("\"id\":\"actor1\"");
        json.Should().Contain("\"peers\":[\"actor1\"]");

        // Multi-dot path through embedded sub-object list ("sub_items.peer.peers")
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "container1",
            SubItems = [
                new TestDataModelWithVirtualLinks.TestSubContainer {
                    Peer = "movie1"
                }
            ]
        });
        var nestedResult = client.Get("container1", new KifaDataOptions {
            LinkedFields = ["sub_items.peer.peers"]
        })!;
        var nestedJson = nestedResult.ToJson();
        nestedJson.Should().Contain("\"id\":\"movie1\"");
        nestedJson.Should().Contain("\"id\":\"actor1\"");
        nestedJson.Should().Contain("\"peers\":[\"movie1\"]");

        // Mixed resolved + missing linked targets serialize as [{ ... }, "nonexistent_actor"]
        // and deserialize cleanly on the client as a dual-typed Link<T> list.
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "mixed_movie",
            Peers = ["actor1", "nonexistent_actor"]
        });
        var mixedJson = client.Get("mixed_movie", new KifaDataOptions {
            LinkedFields = ["Peers"]
        })!.ToJson();
        mixedJson.Should().Contain("\"id\":\"actor1\"")
            .And.Contain(",\"nonexistent_actor\"]");

        var deserializedMixed = mixedJson.FromJson<TestDataModelWithVirtualLinks>()!;
        deserializedMixed.Peers.Should().HaveCount(2);
        deserializedMixed.Peers![0].Id.Should().Be("actor1");
        deserializedMixed.Peers[0].Data!.Data.Should().Be("Actor 1");
        deserializedMixed.Peers[1].Id.Should().Be("nonexistent_actor");
        deserializedMixed.Peers[1].Data.Should().BeNull();
    }

    [Fact]
    public void InvalidLinkedFieldsThrowsArgumentExceptionTest() {
        client.Set(new TestDataModelWithVirtualLinks {
            Id = "sparse_item",
            Data = "plain"
        });

        // Valid Link<T> leaf path succeeds even when intermediate SubItems / Peers are null on this item.
        var sparseResult = client.Get("sparse_item", new KifaDataOptions {
            LinkedFields = ["sub_items.peer.peers"]
        });
        sparseResult.Should().NotBeNull();

        // Non-Link subpart end node fails even when SubItems is null.
        Action subpartEndNode = () => client.Get("sparse_item", new KifaDataOptions {
            LinkedFields = ["sub_items"]
        });
        subpartEndNode.Should().Throw<ArgumentException>()
            .WithMessage("*ends on non-Link type 'TestSubContainer'*");

        // Scalar end node fails.
        Action scalarEndNode = () => client.Get("sparse_item", new KifaDataOptions {
            LinkedFields = ["data"]
        });
        scalarEndNode.Should().Throw<ArgumentException>()
            .WithMessage("*ends on non-Link type 'String'*");

        // Unknown property after a Link<T> fails even when Peers is null.
        Action unknownProperty = () => client.Get("sparse_item", new KifaDataOptions {
            LinkedFields = ["peers.nonexistent"]
        });
        unknownProperty.Should().Throw<ArgumentException>()
            .WithMessage("*property 'nonexistent' was not found*");

        // Empty linked field fails.
        Action emptyField = () => client.Get("sparse_item", new KifaDataOptions {
            LinkedFields = [""]
        });
        emptyField.Should().Throw<ArgumentException>();
    }

    public void Dispose() {
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
