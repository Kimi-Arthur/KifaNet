using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Kifa.Service;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class TestDataModel : DataModel, WithModelId {
    public static string ModelId => "tests";

    public string? Data { get; set; }

    public List<string>? ListData { get; set; }

    public TestDataModel Self { get; set; }
}

public class BasicTests : IDisposable {
    readonly string folder;
    readonly KifaServiceJsonClient<TestDataModel> client = new();

    public BasicTests() {
        folder = $"{Path.GetTempPath()}/{nameof(BasicTests)}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        client.DataFolder = folder;
    }

    [Fact]
    public void GetTest() {
        Directory.CreateDirectory(folder + "/tests");
        var id = nameof(GetTest);
        File.WriteAllText(folder + $"/tests/{id}.json", JsonConvert.SerializeObject(
            new TestDataModel {
                Id = id,
                Data = "good data",
                Self = new TestDataModel {
                    Id = "what",
                    Data = "good what"
                }
            }, KifaJsonSerializerSettings.Pretty));

        var data = client.Get(id);

        Assert.Equal(id, data.Id);
        Assert.Equal("good data", data.Data);

        var nullData = client.Get("not found");
        Assert.Null(nullData);
    }

    [Fact]
    public void SetGetTest() {
        var id = nameof(SetGetTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data",
            Self = new TestDataModel {
                Id = "what",
                Data = "good what"
            }
        });

        var data = client.Get(id);
        Assert.Equal(id, data.Id);
        Assert.Equal("very good data", data.Data);
    }

    [Fact]
    public void LinkTest() {
        var id = nameof(LinkTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var newId = $"{id}_new";
        client.Link(id, newId);

        var data = client.Get(newId);

        data.Id.Should().Be(newId);
        data.Data.Should().Be("very good data");

        data.Metadata.Linking.Links.Should().HaveCount(1).And.Contain(newId);
    }

    [Fact]
    public void LinkToNonExistTest() {
        var id = nameof(LinkToNonExistTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var id1 = $"{id}_1";
        var id2 = $"{id}_2";
        var result = client.Link(id2, id1);

        result.Status.Should().Be(KifaActionStatus.BadRequest);

        client.Get(id1).Should().BeNull();
        client.Get(id2).Should().BeNull();
        client.Get(id).Metadata?.Linking.Should().BeNull();
    }

    [Fact]
    public void LinkFromExistTest() {
        var id = nameof(LinkFromExistTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var newId = $"{id}_new";
        client.Set(new TestDataModel {
            Id = newId,
            Data = "ok data"
        });

        var result = client.Link(newId, id);

        result.Status.Should().Be(KifaActionStatus.BadRequest);

        client.Get(newId).Metadata?.Linking.Should().BeNull();
        client.Get(id).Metadata?.Linking.Should().BeNull();
    }

    [Fact]
    public void LinkToLinkTest() {
        var id = nameof(LinkToLinkTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var id1 = $"{id}_1";
        client.Link(id, id1);
        var id2 = $"{id}_2";
        client.Link(id1, id2);

        client.Get(id).Metadata.Linking.Links.Should().HaveCount(2).And.Contain(id1).And
            .Contain(id2);
        client.Get(id1).Metadata.Linking.Links.Should().HaveCount(2).And.Contain(id1).And
            .Contain(id2);

        var data = client.Get(id2);
        data.Id.Should().Be(id2);
        data.Data.Should().Be("very good data");
        data.Metadata.Linking.Target.Should().Be(id);
    }

    [Fact]
    public void LinkToSameLinkTest() {
        var id = nameof(LinkToSameLinkTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var newId1 = $"{id}_1";
        client.Link(id, newId1);
        var newId2 = $"{id}_2";
        client.Link(newId1, newId2);
        var result = client.Link(id, newId2);

        result.Status.Should().Be(KifaActionStatus.OK);

        client.Get(id).Metadata.Linking.Links.Should().HaveCount(2).And.Contain(newId1).And
            .Contain(newId2);
        client.Get(newId1).Metadata.Linking.Links.Should().HaveCount(2).And.Contain(newId1).And
            .Contain(newId2);

        var data = client.Get(newId2);
        data.Id.Should().Be(newId2);
        data.Data.Should().Be("very good data");
        data.Metadata.Linking.Target.Should().Be(id);
    }

    [Fact]
    public void DeleteTest() {
        var id = nameof(DeleteTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        client.Delete(id);

        var data = client.Get(id);
        data.Should().BeNull();
    }

    [Fact]
    public void DeleteNonExistTest() {
        var id = nameof(DeleteNonExistTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var newId = $"{id}_new";
        var result = client.Delete(newId);

        result.Status.Should().Be(KifaActionStatus.BadRequest);

        var data = client.Get(id);
        Assert.Equal(id, data.Id);
        Assert.Equal("very good data", data.Data);
    }

    [Fact]
    public void DeleteTargetTest() {
        var id = nameof(DeleteTargetTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var newId = $"{id}_new";
        client.Link(id, newId);
        client.Delete(id);

        client.Get(id).Should().BeNull();

        var data = client.Get(newId);

        data.Id.Should().Be(newId);
        data.Data.Should().Be("very good data");

        data.Metadata?.Linking.Should().BeNull();
    }

    [Fact]
    public void DeleteLinkTest() {
        const string id = nameof(DeleteLinkTest);
        const string newId = $"{id}_new";
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        client.Link(id, newId);
        client.Delete(newId);

        client.Get(newId).Should().BeNull();

        var data = client.Get(id);

        data.Id.Should().Be(id);
        data.Data.Should().Be("very good data");

        data.Metadata?.Linking.Should().BeNull();
    }

    [Fact]
    public void UpdateTest() {
        const string id = nameof(UpdateTest);

        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data",
            ListData = new List<string> {
                "abc",
                "bcd"
            }
        });

        client.Update(new TestDataModel {
            Id = id,
            Data = "ok data",
            ListData = new List<string> {
                "bcd",
                "efg"
            }
        });

        var data = client.Get(id);
        data.Id.Should().Be(id);
        data.Data.Should().Be("ok data");
        data.ListData.Should().HaveCount(2).And.ContainInOrder(new[] { "bcd", "efg" });
    }

    [Fact]
    public void UpdateViaTargetTest() {
        var id = nameof(UpdateViaTargetTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data",
            ListData = new List<string> {
                "abc",
                "bcd"
            }
        });

        var newId = $"{id}_new";
        client.Link(id, newId);

        var oldData = client.Get(id);
        oldData.Id.Should().Be(id);
        oldData.Data.Should().Be("very good data");
        oldData.Metadata.Linking.Should().NotBeNull();

        client.Update(new TestDataModel {
            Id = id,
            Data = "ok data"
        });

        oldData = client.Get(id);
        oldData.Id.Should().Be(id);
        oldData.Data.Should().Be("ok data");
        oldData.Metadata.Linking.Should().NotBeNull();

        var newData = client.Get(newId);
        newData.Id.Should().Be(newId);
        newData.Data.Should().Be("ok data");
    }

    [Fact]
    public void UpdateViaLinkTest() {
        const string id = nameof(UpdateViaLinkTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        const string newId = $"{id}_new";
        client.Link(id, newId);

        client.Update(new TestDataModel {
            Id = newId,
            Data = "ok data"
        });

        var data = client.Get(id);
        data.Id.Should().Be(id);
        data.Data.Should().Be("ok data");

        var linkedData = client.Get(newId);
        linkedData.Id.Should().Be(newId);
        linkedData.Data.Should().Be("ok data");
    }

    [Fact]
    public void ListTest() {
        const string id = nameof(ListTest);
        client.Set(new TestDataModel {
            Id = id,
            Data = "very good data"
        });

        var id1 = $"{id}_1";
        client.Set(new TestDataModel {
            Id = id1,
            Data = "ok data"
        });

        var id2 = $"{id}_2";
        client.Link(id, id2);

        var items = client.List().Values.Where(x => x.Id.StartsWith(id));
        items.Should().HaveCount(3).And.Contain(new[] {
            new TestDataModel {
                Id = id,
                Data = "very good data",
                Metadata = new DataMetadata {
                    Linking = new LinkingMetadata {
                        Links = new SortedSet<string> {
                            id2
                        }
                    }
                }
            },
            new TestDataModel {
                Id = id2,
                Data = "very good data",
                Metadata = new DataMetadata {
                    Linking = new LinkingMetadata {
                        Target = id,
                        Links = new SortedSet<string> {
                            id2
                        }
                    }
                }
            },
            new TestDataModel {
                Id = id1,
                Data = "ok data"
            }
        });
    }

    [Fact]
    public void ListEmptyTest() {
        client.List().Values.Where(x => x.Id.StartsWith(nameof(ListEmptyTest))).Should().BeEmpty();
    }

    public void Dispose() {
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
