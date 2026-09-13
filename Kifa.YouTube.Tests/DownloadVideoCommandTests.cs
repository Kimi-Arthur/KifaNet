using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Kifa.Configs;
using Kifa.Service;
using Kifa.Tools.YoutubeUtil.Commands;

namespace Kifa.YouTube.Tests;

[Collection("YouTubeTests")]
public class DownloadVideoCommandTests : IDisposable {
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

    class TestYouTubeVideoServiceClient : BaseKifaServiceClient<YouTubeVideo> {
        readonly Dictionary<string, YouTubeVideo> data = new();

        public override SortedDictionary<string, YouTubeVideo> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubeVideo? Get(string id, bool refresh = false, bool rewrite = false,
            KifaDataOptions? options = null)
            => data.TryGetValue(id, out var item) ? item.Clone() : null;

        public override KifaActionResult Set(YouTubeVideo item) {
            data[item.Id!] = item.Clone();
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(YouTubeVideo item)
            => Set(item);

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId)
            => KifaActionResult.Success();
    }

    readonly KifaServiceClient<YouTubeUploader> originalUploaderClient;
    readonly KifaServiceClient<YouTubeVideo> originalVideoClient;

    public DownloadVideoCommandTests() {
        KifaConfigs.Init();
        originalUploaderClient = YouTubeUploader.Client;
        originalVideoClient = YouTubeVideo.Client;
        YouTubeUploader.Client = new TestYouTubeUploaderServiceClient();
        YouTubeVideo.Client = new TestYouTubeVideoServiceClient();
    }

    public void Dispose() {
        YouTubeUploader.Client = originalUploaderClient;
        YouTubeVideo.Client = originalVideoClient;
    }

    [Fact]
    public void EnsureUploaderInfo_UsesCanonicalUploaderDataWithoutMutatingVideoTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern"
        };
        YouTubeUploader.Client.Set(uploader);

        var video = new YouTubeVideo {
            Id = "vid123",
            Title = "Match Highlights",
            Author = "FC Bayern Munich",
            AuthorId = "@fcbayern"
        };
        YouTubeVideo.Client.Set(video);

        var cmd = new DownloadVideoCommand();
        var resolvedUploader = cmd.EnsureUploaderInfo(video);

        resolvedUploader.Should().NotBeNull();
        resolvedUploader!.Id.Should().Be("@fcbayern");
        resolvedUploader.Name.Should().Be("FC Bayern");

        // Video fields remain original in the data model
        video.Author.Should().Be("FC Bayern Munich");
        video.AuthorId.Should().Be("@fcbayern");

        var desiredName = video.GetDesiredName(uploader: resolvedUploader);
        desiredName.Should().StartWith("FC Bayern.@fcbayern.youtube/");
    }

    [Fact]
    public void EnsureUploaderInfo_LinkedUploaderResolvesCanonicalFolderTest() {
        var target = new YouTubeUploader {
            Id = "@JenniferLopez",
            Name = "Jennifer Lopez"
        };
        YouTubeUploader.Client.Set(target);
        YouTubeUploader.Client.Link("@JenniferLopez", "@JenniferLopezVEVO");

        var video = new YouTubeVideo {
            Id = "d7UBcv8GYUI",
            Title = "I'm Real",
            Author = "Jennifer Lopez",
            AuthorId = "@JenniferLopezVEVO"
        };
        YouTubeVideo.Client.Set(video);

        var cmd = new DownloadVideoCommand();
        var resolvedUploader = cmd.EnsureUploaderInfo(video);

        resolvedUploader.Should().NotBeNull();
        resolvedUploader!.Id.Should().Be("@JenniferLopez");
        resolvedUploader.Name.Should().Be("Jennifer Lopez");

        // Video AuthorId remains untouched as the original upload source
        video.AuthorId.Should().Be("@JenniferLopezVEVO");

        // Folder uses the canonical target uploader
        var desiredName = video.GetDesiredName(uploader: resolvedUploader);
        desiredName.Should().StartWith("Jennifer Lopez.@JenniferLopez.youtube/");
    }

    [Fact]
    public void EnsureUploaderInfo_ResolvesUploaderViaAliasTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern",
            NameAliases = ["FC Bayern Munich"]
        };
        YouTubeUploader.Client.Set(uploader);

        var video = new YouTubeVideo {
            Id = "vid456",
            Title = "Match Highlights",
            Author = "FC Bayern Munich",
            AuthorId = null
        };
        YouTubeVideo.Client.Set(video);

        using var reader = new StringReader("@fcbayern\n\n");
        Console.SetIn(reader);

        var cmd = new DownloadVideoCommand();
        var resolvedUploader = cmd.EnsureUploaderInfo(video);

        resolvedUploader.Should().NotBeNull();
        resolvedUploader!.Id.Should().Be("@fcbayern");
        resolvedUploader.Name.Should().Be("FC Bayern");

        video.Author.Should().Be("FC Bayern Munich");
        video.AuthorId.Should().Be("@fcbayern");
    }

    [Fact]
    public void EnsureUploaderInfo_ResolvesUploaderViaChannelIdTest() {
        var uploader = new YouTubeUploader {
            Id = "@fcbayern",
            Name = "FC Bayern",
            ChannelId = "UCk5b0K_3ABC123"
        };
        YouTubeUploader.Client.Set(uploader);

        var video = new YouTubeVideo {
            Id = "vid789",
            Title = "Match Highlights",
            Author = "Some Channel",
            AuthorId = "UCk5b0K_3ABC123"
        };
        YouTubeVideo.Client.Set(video);

        var cmd = new DownloadVideoCommand();
        var resolvedUploader = cmd.EnsureUploaderInfo(video);

        resolvedUploader.Should().NotBeNull();
        resolvedUploader!.Id.Should().Be("@fcbayern");
        resolvedUploader.Name.Should().Be("FC Bayern");

        video.Author.Should().Be("Some Channel");
        video.AuthorId.Should().Be("UCk5b0K_3ABC123");

        var desiredName = video.GetDesiredName(uploader: resolvedUploader);
        desiredName.Should().StartWith("FC Bayern.@fcbayern.youtube/");
    }

    [Fact]
    public void EnsureUploaderInfo_PromptsForMissingAuthorAndAuthorIdTest() {
        var video = new YouTubeVideo {
            Id = "vid999",
            Title = "Independent Documentary",
            Author = null,
            AuthorId = null
        };
        YouTubeVideo.Client.Set(video);

        var originalIn = Console.In;
        try {
            // Provide Author name then AuthorId via console input.
            // In Confirm(string, string): user enters value + newline, then enter + newline to accept.
            using var reader = new StringReader("Documentary Studio\n\n@docstudio\n\n");
            Console.SetIn(reader);

            var cmd = new DownloadVideoCommand();
            cmd.EnsureUploaderInfo(video);

            video.Author.Should().Be("Documentary Studio");
            video.AuthorId.Should().Be("@docstudio");

            var createdUploader = YouTubeUploader.Client.Get("@docstudio");
            createdUploader.Should().NotBeNull();
            createdUploader!.Name.Should().Be("Documentary Studio");

            var updatedVideo = YouTubeVideo.Client.Get("vid999");
            updatedVideo.Should().NotBeNull();
            updatedVideo!.Author.Should().Be("Documentary Studio");
            updatedVideo.AuthorId.Should().Be("@docstudio");
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void EnsureUploaderInfo_PromptsForMissingAuthorOnlyTest() {
        var video = new YouTubeVideo {
            Id = "vid888",
            Title = "Tech Review",
            Author = null,
            AuthorId = "@techchannel"
        };
        YouTubeVideo.Client.Set(video);

        var originalIn = Console.In;
        try {
            using var reader = new StringReader("Tech Channel\n\n");
            Console.SetIn(reader);

            var cmd = new DownloadVideoCommand();
            cmd.EnsureUploaderInfo(video);

            video.Author.Should().Be("Tech Channel");
            video.AuthorId.Should().Be("@techchannel");

            var createdUploader = YouTubeUploader.Client.Get("@techchannel");
            createdUploader.Should().NotBeNull();
            createdUploader!.Name.Should().Be("Tech Channel");
        } finally {
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void EnsureUploaderInfo_PromptsForMissingAuthorIdOnlyTest() {
        var video = new YouTubeVideo {
            Id = "vid777",
            Title = "Cooking Video",
            Author = "Cooking Channel",
            AuthorId = null
        };
        YouTubeVideo.Client.Set(video);

        var originalIn = Console.In;
        try {
            using var reader = new StringReader("@cookingchannel\n\n");
            Console.SetIn(reader);

            var cmd = new DownloadVideoCommand();
            cmd.EnsureUploaderInfo(video);

            video.Author.Should().Be("Cooking Channel");
            video.AuthorId.Should().Be("@cookingchannel");

            var createdUploader = YouTubeUploader.Client.Get("@cookingchannel");
            createdUploader.Should().NotBeNull();
            createdUploader!.Name.Should().Be("Cooking Channel");
        } finally {
            Console.SetIn(originalIn);
        }
    }
}
