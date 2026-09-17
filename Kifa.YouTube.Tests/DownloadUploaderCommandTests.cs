using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Kifa.Configs;
using Kifa.IO;
using Kifa.IO.StorageClients;
using Kifa.Service;
using Kifa.Tools.YoutubeUtil.Commands;

namespace Kifa.YouTube.Tests;

[Collection("YouTubeTests")]
public class DownloadUploaderCommandTests : IDisposable {
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

    class TestYouTubeUploaderServiceClient : BaseKifaServiceClient<YouTubeUploader> {
        readonly Dictionary<string, YouTubeUploader> data = new();

        public override SortedDictionary<string, YouTubeUploader> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubeUploader? Get(string id, bool refresh = false, bool rewrite = false,
            KifaDataOptions? options = null)
            => data.TryGetValue(id, out var item) ? item.Clone() : null;

        public override KifaActionResult Set(YouTubeUploader item) {
            data[item.Id!] = item.Clone();
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

    class TestYouTubeUploaderVideosServiceClient : BaseKifaServiceClient<YouTubeUploaderVideos> {
        readonly Dictionary<string, YouTubeUploaderVideos> data = new();

        public override SortedDictionary<string, YouTubeUploaderVideos> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubeUploaderVideos? Get(string id, bool refresh = false, bool rewrite = false,
            KifaDataOptions? options = null)
            => data.TryGetValue(id, out var item) ? item.Clone() : null;

        public override KifaActionResult Set(YouTubeUploaderVideos item) {
            data[item.Id!] = item.Clone();
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(YouTubeUploaderVideos item)
            => Set(item);

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId)
            => KifaActionResult.Success();
    }

    class TestFileInformationServiceClient : BaseKifaServiceClient<FileInformation>,
        FileInformationServiceClient {
        readonly Dictionary<string, FileInformation> data = new();

        public void AddFile(string id) {
            data[id] = new FileInformation {
                Id = id,
                Locations = new SortedDictionary<string, DateTime?> {
                    ["/local/path"] = DateTime.UtcNow
                }
            };
        }

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override FileInformation? Get(string id, bool refresh = false, bool rewrite = false,
            KifaDataOptions? options = null)
            => data.GetValueOrDefault(id);

        public override KifaActionResult Set(FileInformation item) {
            data[item.Id!] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(FileInformation item) {
            data[item.Id!] = item;
            return KifaActionResult.Success();
        }

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId)
            => KifaActionResult.Success();

        public List<FolderInfo> GetFolder(string folder, List<string> targets) => [];

        public List<string> ListFolder(string folder, bool recursive = false) {
            folder = folder.TrimEnd('/') + "/";
            return data.Keys.Where(k => k.StartsWith(folder)).ToList();
        }

        public KifaActionResult AddLocation(string id, string location, bool verified = false)
            => KifaActionResult.Success();

        public KifaActionResult RemoveLocation(string id, string location)
            => KifaActionResult.Success();
    }

    readonly string tempDir;
    readonly KifaServiceClient<YouTubeVideo> originalVideoClient;
    readonly KifaServiceClient<YouTubeUploader> originalUploaderClient;
    readonly KifaServiceClient<YouTubeUploaderVideos> originalUploaderVideosClient;
    readonly FileInformationServiceClient originalFileInfoClient;
    readonly TestFileInformationServiceClient testFileInfoClient;

    public DownloadUploaderCommandTests() {
        KifaConfigs.Init();
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_yt_up_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["yt_up_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };

        originalVideoClient = YouTubeVideo.Client;
        originalUploaderClient = YouTubeUploader.Client;
        originalUploaderVideosClient = YouTubeUploaderVideos.Client;
        originalFileInfoClient = FileInformation.Client;

        YouTubeVideo.Client = new TestYouTubeVideoServiceClient();
        YouTubeUploader.Client = new TestYouTubeUploaderServiceClient();
        YouTubeUploaderVideos.Client = new TestYouTubeUploaderVideosServiceClient();
        testFileInfoClient = new TestFileInformationServiceClient();
        FileInformation.Client = testFileInfoClient;
    }

    public void Dispose() {
        YouTubeVideo.Client = originalVideoClient;
        YouTubeUploader.Client = originalUploaderClient;
        YouTubeUploaderVideos.Client = originalUploaderVideosClient;
        FileInformation.Client = originalFileInfoClient;
        FileStorageClient.ServerConfigs.Remove("yt_up_test_temp");

        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void DownloadUploader_BreakOnExisting_StopsEarlyWhenEncounteringExistingVideoTest() {
        var uploader = new YouTubeUploader {
            Id = "@creator",
            Name = "Creator"
        };
        YouTubeUploader.Client.Set(uploader);

        var uploaderVideos = new YouTubeUploaderVideos {
            Id = "@creator",
            Videos = ["vid1", "vid2"]
        };
        YouTubeUploaderVideos.Client.Set(uploaderVideos);

        var video1 = new YouTubeVideo {
            Id = "vid1",
            Title = "Video 1",
            Author = "Creator",
            AuthorId = "@creator"
        };
        var video2 = new YouTubeVideo {
            Id = "vid2",
            Title = "Video 2",
            Author = "Creator",
            AuthorId = "@creator"
        };
        YouTubeVideo.Client.Set(video1);
        YouTubeVideo.Client.Set(video2);

        // Uploader videos order is vid1, vid2 (so newest first by default is vid1 in uploader list)
        // Register vid1 canonical file as existing in the system via FileInformation
        testFileInfoClient.AddFile($"{YoutubeCommand.RepoPath}/vid1.mp4");

        var cmd = new DownloadUploaderCommand {
            UploaderId = "@creator",
            OutputFolder = $"{tempDir}/output",
            BreakOnExisting = true
        };

        var exitCode = cmd.Execute();
        exitCode.Should().Be(0);
        cmd.Results.Should().HaveCount(1);
        cmd.Results[0].item.Should().Be("vid1");
    }
}
