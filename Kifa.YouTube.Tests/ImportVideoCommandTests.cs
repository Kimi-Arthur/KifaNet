using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Kifa.Configs;
using Kifa.Service;
using Kifa.Tools.YoutubeUtil.Commands;

namespace Kifa.YouTube.Tests;

[Collection("YouTubeTests")]
public class ImportVideoCommandTests : IDisposable {
    class TestYouTubeVideoServiceClient : BaseKifaServiceClient<YouTubeVideo> {
        readonly Dictionary<string, YouTubeVideo> data = new();

        public override SortedDictionary<string, YouTubeVideo> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubeVideo? Get(string id, bool refresh = false, bool rewrite = false,
            KifaDataOptions? options = null)
            => data.GetValueOrDefault(id)?.Clone();

        public override KifaActionResult Set(YouTubeVideo item) {
            data[item.Id!] = item.Clone();
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(YouTubeVideo item) {
            data[item.Id!] = item.Clone();
            return KifaActionResult.Success();
        }

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId)
            => KifaActionResult.Success();
    }

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

    readonly KifaServiceClient<YouTubeVideo> originalVideoClient;
    readonly KifaServiceClient<YouTubeUploader> originalUploaderClient;

    public ImportVideoCommandTests() {
        KifaConfigs.Init();
        originalVideoClient = YouTubeVideo.Client;
        originalUploaderClient = YouTubeUploader.Client;
        YouTubeVideo.Client = new TestYouTubeVideoServiceClient();
        YouTubeUploader.Client = new TestYouTubeUploaderServiceClient();
    }

    public void Dispose() {
        YouTubeVideo.Client = originalVideoClient;
        YouTubeUploader.Client = originalUploaderClient;
    }

    [Fact]
    public void FromMediaInfoTextExampleTest() {
        var uploader = new YouTubeUploader {
            Id = "@pharkil",
            Name = "pharkil"
        };
        YouTubeUploader.Client.Set(uploader);

        var mediaInfo = """
            General
            Complete name                            : wjusUSpm3C0.mp4
            Format                                   : MPEG-4
            Format profile                           : Base Media
            Codec ID                                 : isom (isom/iso2/avc1/mp41)
            File size                                : 191 MiB
            Duration                                 : 3 min 26 s
            Overall bit rate mode                    : Variable
            Overall bit rate                         : 7 760 kb/s
            Frame rate                               : 29.970 FPS
            Title                                    : [직캠/Fancam] 140621 크레용팝(Crayon Pop) 어이(Uh-ee) (엘린) @ 청주 크롬콘서트
            Performer                                : pharkil
            Description                              : Canon 70D, Sigma 120-300mm, 1.4X Extender
            Recorded date                            : 20140621
            Encoded date                             : 2014-06-20 17:55:08 UTC
            Tagged date                              : 2014-06-20 17:55:08 UTC
            Writing application                      : Lavf56.40.101
            Cover                                    : Yes
            Cover type                               : Cover
            Comment                                  : Canon 70D, Sigma 120-300mm, 1.4X Extender
            """;

        var video = YouTubeVideo.FromMediaInfo(mediaInfo);
        video.Should().NotBeNull();
        video!.Id.Should().Be("wjusUSpm3C0");
        video.Title.Should().Be("[직캠/Fancam] 140621 크레용팝(Crayon Pop) 어이(Uh-ee) (엘린) @ 청주 크롬콘서트");
        video.Author.Should().Be("pharkil");
        video.AuthorId.Should().Be("@pharkil");
        video.Description.Should().Be("Canon 70D, Sigma 120-300mm, 1.4X Extender");
        video.UploadDate.Should().Be(Date.Parse("2014-06-21"));
        video.Duration.Should().Be(new TimeSpan(0, 3, 26));
        video.Codec.Should().BeNull();
        video.Width.Should().Be(0);
        video.Height.Should().Be(0);
        video.Fps.Should().Be(0);
    }

    [Fact]
    public void FromMediaInfoJsonTest() {
        var json = """
            {
              "media": {
                "track": [
                  {
                    "@type": "General",
                    "CompleteName": "test_folder/wjusUSpm3C0.mp4",
                    "Title": "Sample Title",
                    "Performer": "Sample Artist",
                    "Description": "Sample Description",
                    "Recorded_Date": "20200515",
                    "Duration": "125.5"
                  }
                ]
              }
            }
            """;

        var video = YouTubeVideo.FromMediaInfo(json);
        video.Should().NotBeNull();
        video!.Id.Should().Be("wjusUSpm3C0");
        video.Title.Should().Be("Sample Title");
        video.Author.Should().Be("Sample Artist");
        video.Description.Should().Be("Sample Description");
        video.UploadDate.Should().Be(Date.Parse("2020-05-15"));
        video.Duration.Should().Be(TimeSpan.FromSeconds(125.5));
    }

    [Fact]
    public void FromFfprobeJsonTest() {
        var json = """
            {
              "format": {
                "filename": "/media/wjusUSpm3C0.mp4",
                "duration": "206.000000",
                "tags": {
                  "title": "FFprobe Title",
                  "artist": "FFprobe Artist",
                  "comment": "FFprobe Comment",
                  "date": "2018-09-12"
                }
              }
            }
            """;

        var video = YouTubeVideo.FromMediaInfo(json);
        video.Should().NotBeNull();
        video!.Id.Should().Be("wjusUSpm3C0");
        video.Title.Should().Be("FFprobe Title");
        video.Author.Should().Be("FFprobe Artist");
        video.Description.Should().Be("FFprobe Comment");
        video.UploadDate.Should().Be(Date.Parse("2018-09-12"));
        video.Duration.Should().Be(TimeSpan.FromSeconds(206));
    }

    static string CreateTestVideoFile(string id, string title, string artist, string comment,
        string date) {
        Directory.CreateDirectory(".agent_temp");
        var filePath = Path.GetFullPath($".agent_temp/{id}.mp4");
        Executor.Run("ffmpeg",
            $"-f lavfi -i color=c=black:s=160x120:d=1 -metadata title=\"{title}\" -metadata artist=\"{artist}\" -metadata comment=\"{comment}\" -metadata date=\"{date}\" \"{filePath}\" -y");
        return filePath;
    }

    [Fact]
    public void ImportVideoCommand_AddsNewValuesWithSelectManyTest() {
        var filePath = CreateTestVideoFile("test_add_id", "Video Title 1", "pharkil",
            "Video Description 1", "20210501");

        var uploader = new YouTubeUploader {
            Id = "@pharkil",
            Name = "pharkil"
        };
        YouTubeUploader.Client.Set(uploader);

        var cmd = new ImportVideoCommand {
            Files = [filePath],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var saved = YouTubeVideo.Client.Get("test_add_id");
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Video Title 1");
        saved.Author.Should().Be("pharkil");
        saved.AuthorId.Should().Be("@pharkil");
        saved.Description.Should().Be("Video Description 1");
        saved.UploadDate.Should().Be(Date.Parse("2021-05-01"));
        saved.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void ImportVideoCommand_OverridesExistingValuesWithSelectManyTest() {
        var filePath = CreateTestVideoFile("test_overr1", "New Video Title", "pharkil",
            "New Description", "20220601");

        var uploader = new YouTubeUploader {
            Id = "@pharkil",
            Name = "pharkil"
        };
        YouTubeUploader.Client.Set(uploader);

        var existing = new YouTubeVideo {
            Id = "test_overr1",
            Title = "Old Video Title",
            Author = "Old Author",
            Description = "Old Description",
            UploadDate = Date.Parse("2010-01-01")
        };
        YouTubeVideo.Client.Set(existing);

        var cmd = new ImportVideoCommand {
            Files = [filePath],
            AutoConfirmDefault = true
        };

        var result = cmd.Execute();
        result.Should().Be(0);

        var updated = YouTubeVideo.Client.Get("test_overr1");
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("New Video Title");
        updated.Author.Should().Be("pharkil");
        updated.AuthorId.Should().Be("@pharkil");
        updated.Description.Should().Be("New Description");
        updated.UploadDate.Should().Be(Date.Parse("2022-06-01"));
    }

    [Fact]
    public void ImportVideoCommand_SelectiveConfirmationTest() {
        var filePath = CreateTestVideoFile("test_selec1", "Selective Title", "pharkil",
            "Selective Description", "20230701");

        var existing = new YouTubeVideo {
            Id = "test_selec1",
            Title = "Original Title",
            UploadDate = Date.Parse("2010-01-01")
        };
        YouTubeVideo.Client.Set(existing);

        var originalIn = Console.In;
        try {
            // Choice 1 is Title. Entering 1 then confirming with Enter.
            Console.SetIn(new StringReader("1\n\n"));

            var cmd = new ImportVideoCommand {
                Files = [filePath]
            };

            var result = cmd.Execute();
            result.Should().Be(0);

            var updated = YouTubeVideo.Client.Get("test_selec1");
            updated.Should().NotBeNull();
            // Title is updated because choice 1 was selected
            updated!.Title.Should().Be("Selective Title");
            // UploadDate remains unchanged because choice for UploadDate was not selected
            updated.UploadDate.Should().Be(Date.Parse("2010-01-01"));
            // Description was not selected
            updated.Description.Should().BeNull();
        } finally {
            Console.SetIn(originalIn);
        }
    }
}
