using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Kifa.Service;
using Newtonsoft.Json;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class TestDataModel : DataModel<TestDataModel> {
    public const string ModelId = "tests";

    public string? Data { get; set; }

    public List<string>? ListData { get; set; }
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
        File.WriteAllText(folder + "/tests/test.json", JsonConvert.SerializeObject(
            new TestDataModel {
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
    public void LinkToNonExistTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        var result = client.Link("test1", "new_test");

        result.Status.Should().Be(KifaActionStatus.BadRequest);

        client.Get("new_test").Should().BeNull();
        client.Get("test1").Should().BeNull();
        client.Get("test").Metadata?.Linking.Should().BeNull();
    }

    [Fact]
    public void LinkFromExistTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Set(new TestDataModel {
            Id = "test1",
            Data = "ok data"
        });

        var result = client.Link("test1", "test");

        result.Status.Should().Be(KifaActionStatus.BadRequest);

        client.Get("test1").Metadata?.Linking.Should().BeNull();
        client.Get("test").Metadata?.Linking.Should().BeNull();
    }

    [Fact]
    public void LinkToLinkTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "test1");
        client.Link("test1", "test2");

        client.Get("test").Metadata.Linking.Links.Should().HaveCount(2).And.Contain("test1").And
            .Contain("test2");
        client.Get("test1").Metadata.Linking.Links.Should().HaveCount(2).And.Contain("test1").And
            .Contain("test2");

        var data = client.Get("test2");
        data.Id.Should().Be("test2");
        data.Data.Should().Be("very good data");
        data.Metadata.Linking.Target.Should().Be("test");
    }

    [Fact]
    public void LinkToSameLinkTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Link("test", "test1");
        client.Link("test1", "test2");
        var result = client.Link("test", "test2");

        result.Status.Should().Be(KifaActionStatus.OK);

        client.Get("test").Metadata.Linking.Links.Should().HaveCount(2).And.Contain("test1").And
            .Contain("test2");
        client.Get("test1").Metadata.Linking.Links.Should().HaveCount(2).And.Contain("test1").And
            .Contain("test2");

        var data = client.Get("test2");
        data.Id.Should().Be("test2");
        data.Data.Should().Be("very good data");
        data.Metadata.Linking.Target.Should().Be("test");
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
            Data = "very good data",
            ListData = new List<string> {
                "abc",
                "bcd"
            }
        });

        client.Update(new TestDataModel {
            Id = "test",
            Data = "ok data",
            ListData = new List<string> {
                "bcd",
                "efg"
            }
        });

        var data = client.Get("test");
        data.Id.Should().Be("test");
        data.Data.Should().Be("ok data");
        data.ListData.Should().HaveCount(2).And.ContainInOrder(new[] { "bcd", "efg" });
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

    [Fact]
    public void ListTest() {
        client.Set(new TestDataModel {
            Id = "test",
            Data = "very good data"
        });

        client.Set(new TestDataModel {
            Id = "test1",
            Data = "ok data"
        });

        client.Link("test", "new_test");

        var items = client.List();
        items.Should().HaveCount(3).And.Contain(new KeyValuePair<string, TestDataModel>[] {
            new("test", new TestDataModel {
                Id = "test",
                Data = "very good data",
                Metadata = new DataMetadata {
                    Linking = new LinkingMetadata {
                        Links = new SortedSet<string> {
                            "new_test"
                        }
                    }
                }
            }),
            new("new_test", new TestDataModel {
                Id = "new_test",
                Data = "very good data",
                Metadata = new DataMetadata {
                    Linking = new LinkingMetadata {
                        Target = "test",
                        Links = new SortedSet<string> {
                            "new_test"
                        }
                    }
                }
            }),
            new("test1", new TestDataModel {
                Id = "test1",
                Data = "ok data"
            })
        });
    }

    [Fact]
    public void ListEmptyTest() {
        client.List().Should().BeEmpty();
    }

    public void Dispose() {
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
