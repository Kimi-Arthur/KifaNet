using System;
using System.Collections.Generic;
using FluentAssertions;
using Kifa.Configs;
using Kifa.Service;
using Kifa.Tools.YoutubeUtil.Commands;
using Xunit;

namespace Kifa.YouTube.Tests;

[Collection("YouTubeTests")]
public class AddUploaderNameCommandTests : IDisposable {
    class TestYouTubeUploaderServiceClient : BaseKifaServiceClient<YouTubeUploader> {
        readonly Dictionary<string, YouTubeUploader> data = new();

        public override SortedDictionary<string, YouTubeUploader> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubeUploader? Get(string id, bool refresh = false, bool rewrite = false,
            KifaDataOptions? options = null) {
            if (!data.TryGetValue(id, out var item)) {
                return null;
            }

            if (item.Metadata?.Linking?.Target != null) {
                var target = data.GetValueOrDefault(item.Metadata.Linking.Target);
                if (target != null) {
                    var clone = target.Clone();
                    clone.Metadata ??= new DataMetadata();
                    clone.Metadata.Linking ??= new LinkingMetadata();
                    clone.Metadata.Linking.Target = target.Id;
                    clone.Id = id;
                    return clone;
                }
            }

            return item.Clone();
        }

        public override KifaActionResult Set(YouTubeUploader item) {
            data[item.Id!] = item.Clone();
            foreach (var virtualItem in item.GetVirtualItems()) {
                data[virtualItem] = new YouTubeUploader {
                    Id = virtualItem,
                    Metadata = new DataMetadata {
                        Linking = new LinkingMetadata {
                            Target = item.Id
                        }
                    }
                };
            }

            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(YouTubeUploader item)
            => Set(item);

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId)
            => KifaActionResult.Success();
    }

    readonly KifaServiceClient<YouTubeUploader> originalClient;

    public AddUploaderNameCommandTests() {
        KifaConfigs.Init();
        originalClient = YouTubeUploader.Client;
        YouTubeUploader.Client = new TestYouTubeUploaderServiceClient();
    }

    public void Dispose() {
        YouTubeUploader.Client = originalClient;
    }

    [Fact]
    public void AddAliasToExistingUploaderTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern Munich"
        };
        YouTubeUploader.Client.Set(uploader);

        var cmd = new AddUploaderNameCommand {
            UploaderId = "@fcbayern",
            Names = ["Bayern Munich", "FCB"]
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@fcbayern");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("FC Bayern Munich");
        updated.NameAliases.Should().BeEquivalentTo(["Bayern Munich", "FCB"]);
    }

    [Fact]
    public void LocateByCanonicalNameAndAddAliasTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern Munich"
        };
        YouTubeUploader.Client.Set(uploader);

        // Locate using the primary canonical Name "FC Bayern Munich"
        var cmd = new AddUploaderNameCommand {
            UploaderId = "FC Bayern Munich",
            Names = ["Bayern Munich"]
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@fcbayern");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("FC Bayern Munich");
        updated.NameAliases.Should().BeEquivalentTo(["Bayern Munich"]);
    }

    [Fact]
    public void LocateByExistingAliasAndAddAnotherAliasTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern Munich",
            NameAliases = ["Bayern Munich"]
        };
        YouTubeUploader.Client.Set(uploader);

        // Locate using an existing alias "Bayern Munich" to add another alias "FCB"
        var cmd = new AddUploaderNameCommand {
            UploaderId = "Bayern Munich",
            Names = ["FCB"]
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@fcbayern");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("FC Bayern Munich");
        updated.NameAliases.Should().BeEquivalentTo(["Bayern Munich", "FCB"]);
    }

    [Fact]
    public void LocateByBareHandleTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern Munich"
        };
        YouTubeUploader.Client.Set(uploader);

        // Locate using bare handle "fcbayern" (without @)
        var cmd = new AddUploaderNameCommand {
            UploaderId = "fcbayern",
            Names = ["Die Roten"]
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@fcbayern");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("FC Bayern Munich");
        updated.NameAliases.Should().BeEquivalentTo(["Die Roten"]);
    }

    [Fact]
    public void AddCanonicalNameToExistingUploaderTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern Munich",
            NameAliases = ["FCB"]
        };
        YouTubeUploader.Client.Set(uploader);

        var cmd = new AddUploaderNameCommand {
            UploaderId = "@fcbayern",
            Names = ["FC Bayern München", "Die Bayern"],
            AsCanonical = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@fcbayern");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("FC Bayern München");
        updated.NameAliases.Should().BeEquivalentTo(["FCB", "FC Bayern Munich", "Die Bayern"]);
    }

    [Fact]
    public void AddNameToUploaderWithNullNameTest() {
        var uploader = new YouTubeUploader {
            Id = "@newchannel"
        };
        YouTubeUploader.Client.Set(uploader);

        var cmd = new AddUploaderNameCommand {
            UploaderId = "@newchannel",
            Names = ["New Channel"]
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@newchannel");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Channel");
        updated.NameAliases.Should().BeEmpty();
    }
}
