using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Kifa.Configs;
using Kifa.Service;
using Kifa.Tools.YoutubeUtil.Commands;

namespace Kifa.YouTube.Tests;

[Collection("YouTubeTests")]
public class AddUploaderNameCommandTests : IDisposable {
    class TestYouTubeUploaderServiceClient : BaseKifaServiceClient<YouTubeUploader> {
        readonly Dictionary<string, YouTubeUploader> data = new();

        public override SortedDictionary<string, YouTubeUploader> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubeUploader? Get(string id, KifaDataOptions? options = null) {
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

        public override KifaActionResult Link(string targetId, string linkId) {
            if (!data.TryGetValue(targetId, out var target)) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = $"Target {targetId} doesn't exist."
                };
            }

            var realTargetId = target.RealId;
            data[linkId] = new YouTubeUploader {
                Id = linkId,
                Metadata = new DataMetadata {
                    Linking = new LinkingMetadata {
                        Target = realTargetId
                    }
                }
            };

            target.Metadata ??= new DataMetadata();
            target.Metadata.Linking ??= new LinkingMetadata();
            target.Metadata.Linking.Links ??= new SortedSet<string>();
            target.Metadata.Linking.Links.Add(linkId);
            return KifaActionResult.Success();
        }
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
            Names = ["Bayern Munich", "FCB"],
            AutoConfirmDefault = true
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
            Names = ["Bayern Munich"],
            AutoConfirmDefault = true
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
            Names = ["FCB"],
            AutoConfirmDefault = true
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
            Names = ["Die Roten"],
            AutoConfirmDefault = true
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
            AsCanonical = true,
            AutoConfirmDefault = true
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
            Names = ["New Channel"],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@newchannel");
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Channel");
        updated.NameAliases.Should().BeEmpty();
    }

    [Fact]
    public void AddSingleArgumentToCreateNewUploaderTest() {
        var cmd = new AddUploaderNameCommand {
            UploaderId = "@brandnewchannel",
            Names = [],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@brandnewchannel");
        updated.Should().NotBeNull();
        updated!.Id.Should().Be("@brandnewchannel");
        updated.Name.Should().BeNull();
        updated.NameAliases.Should().BeEmpty();
    }

    [Fact]
    public void AddSingleArgumentToExistingUploaderTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern Munich"
        };
        YouTubeUploader.Client.Set(uploader);

        var cmd = new AddUploaderNameCommand {
            UploaderId = "@fcbayern",
            Names = [],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeUploader.Client.Get("@fcbayern");
        updated.Should().NotBeNull();
        updated!.Id.Should().Be("@fcbayern");
        updated.Name.Should().Be("FC Bayern Munich");
    }

    [Fact]
    public void AddUploaderByNameWithPromptForIdTest() {
        var originalIn = Console.In;
        try {
            using var reader = new StringReader("@fcbayern\n\ny\n");
            Console.SetIn(reader);

            var cmd = new AddUploaderNameCommand {
                UploaderId = "FC Bayern Munich",
                Names = ["Bayern Munich", "FCB"]
            };

            var result = cmd.Execute();
            result.Should().Be(0);

            var updated = YouTubeUploader.Client.Get("@fcbayern");
            updated.Should().NotBeNull();
            updated!.Id.Should().Be("@fcbayern");
            updated.Name.Should().Be("FC Bayern Munich");
            updated.NameAliases.Should().BeEquivalentTo(["Bayern Munich", "FCB"]);
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void AddUploaderCancelledByUserTest() {
        var originalIn = Console.In;
        try {
            using var reader = new StringReader("n\n");
            Console.SetIn(reader);

            var cmd = new AddUploaderNameCommand {
                UploaderId = "@cancelledchannel",
                Names = ["Cancelled Name"]
            };

            var result = cmd.Execute();
            result.Should().Be(0);

            var updated = YouTubeUploader.Client.Get("@cancelledchannel");
            updated.Should().BeNull();
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void AddUploaderWithHandleLinksTest() {
        var uploader = new YouTubeUploader {
            Id = "@JenniferLopez",
            Name = "Jennifer Lopez"
        };
        YouTubeUploader.Client.Set(uploader);

        var cmd = new AddUploaderNameCommand {
            UploaderId = "@JenniferLopez",
            Names = ["@JenniferLopezVEVO"],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var resolved = YouTubeUploader.Get("@JenniferLopezVEVO");
        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be("@JenniferLopez");
        resolved.Name.Should().Be("Jennifer Lopez");
    }

    [Fact]
    public void AddUploaderWithNamesAndLinksSimultaneouslyTest() {
        var uploader = new YouTubeUploader {
            Id = "@JenniferLopez"
        };
        YouTubeUploader.Client.Set(uploader);

        var cmd = new AddUploaderNameCommand {
            UploaderId = "@JenniferLopez",
            Names = ["Jennifer Lopez", "J.Lo", "@JenniferLopezVEVO", "UCx1f1u4XlFFr0YgqF3wB4lQ"],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var target = YouTubeUploader.Client.Get("@JenniferLopez");
        target.Should().NotBeNull();
        target!.Name.Should().Be("Jennifer Lopez");
        target.NameAliases.Should().BeEquivalentTo(["J.Lo"]);

        var resolvedHandle = YouTubeUploader.Get("@JenniferLopezVEVO");
        resolvedHandle.Should().NotBeNull();
        resolvedHandle!.Id.Should().Be("@JenniferLopez");

        var resolvedChannelId = YouTubeUploader.Get("UCx1f1u4XlFFr0YgqF3wB4lQ");
        resolvedChannelId.Should().NotBeNull();
        resolvedChannelId!.Id.Should().Be("@JenniferLopez");
    }

    [Fact]
    public void AddUploaderMergesStandaloneRecordWhenLinkingTest() {
        var target = new YouTubeUploader {
            Id = "@JenniferLopez",
            Name = "Jennifer Lopez"
        };
        YouTubeUploader.Client.Set(target);

        var standalone = new YouTubeUploader {
            Id = "@JenniferLopezVEVO",
            Name = "J.Lo VEVO",
            NameAliases = ["JenniferLopezVEVO"]
        };
        YouTubeUploader.Client.Set(standalone);

        var cmd = new AddUploaderNameCommand {
            UploaderId = "@JenniferLopez",
            Names = ["@JenniferLopezVEVO"],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updatedTarget = YouTubeUploader.Client.Get("@JenniferLopez");
        updatedTarget.Should().NotBeNull();
        updatedTarget!.NameAliases.Should().Contain("J.Lo VEVO");
        updatedTarget.NameAliases.Should().Contain("JenniferLopezVEVO");

        var resolved = YouTubeUploader.Get("@JenniferLopezVEVO");
        resolved.Should().NotBeNull();
        resolved!.Id.Should().Be("@JenniferLopez");
    }
}
