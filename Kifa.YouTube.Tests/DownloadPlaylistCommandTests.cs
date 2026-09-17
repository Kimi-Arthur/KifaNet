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
public class DownloadPlaylistCommandTests : IDisposable {
    class TestYouTubePlaylistServiceClient : BaseKifaServiceClient<YouTubePlaylist> {
        readonly Dictionary<string, YouTubePlaylist> data = new();

        public override SortedDictionary<string, YouTubePlaylist> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubePlaylist? Get(string id, KifaDataOptions? options = null)
            => data.TryGetValue(id, out var item) ? item.Clone() : null;

        public override KifaActionResult Set(YouTubePlaylist item) {
            data[item.Id!] = item.Clone();
            return KifaActionResult.Success();
        }

        public override KifaActionResult Update(YouTubePlaylist item)
            => Set(item);

        public override KifaActionResult Delete(string id) {
            data.Remove(id);
            return KifaActionResult.Success();
        }

        public override KifaActionResult Link(string targetId, string linkId)
            => KifaActionResult.Success();
    }

    class TestYouTubeVideoServiceClient : BaseKifaServiceClient<YouTubeVideo> {
        readonly Dictionary<string, YouTubeVideo> data = new();

        public override SortedDictionary<string, YouTubeVideo> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null)
            => new(data);

        public override YouTubeVideo? Get(string id, KifaDataOptions? options = null)
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

        public override YouTubeUploader? Get(string id, KifaDataOptions? options = null)
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

        public override FileInformation? Get(string id, KifaDataOptions? options = null)
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
    readonly KifaServiceClient<YouTubePlaylist> originalPlaylistClient;
    readonly KifaServiceClient<YouTubeVideo> originalVideoClient;
    readonly KifaServiceClient<YouTubeUploader> originalUploaderClient;
    readonly FileInformationServiceClient originalFileInfoClient;
    readonly TestFileInformationServiceClient testFileInfoClient;

    public DownloadPlaylistCommandTests() {
        KifaConfigs.Init();
        tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"kifa_yt_pl_test_{Guid.NewGuid()}"))
            .Replace('\\', '/');
        Directory.CreateDirectory(tempDir);
        FileStorageClient.ServerConfigs["yt_pl_test_temp"] = new ServerConfig {
            Prefix = tempDir
        };

        originalPlaylistClient = YouTubePlaylist.Client;
        originalVideoClient = YouTubeVideo.Client;
        originalUploaderClient = YouTubeUploader.Client;
        originalFileInfoClient = FileInformation.Client;

        YouTubePlaylist.Client = new TestYouTubePlaylistServiceClient();
        YouTubeVideo.Client = new TestYouTubeVideoServiceClient();
        YouTubeUploader.Client = new TestYouTubeUploaderServiceClient();
        testFileInfoClient = new TestFileInformationServiceClient();
        FileInformation.Client = testFileInfoClient;
    }

    public void Dispose() {
        YouTubePlaylist.Client = originalPlaylistClient;
        YouTubeVideo.Client = originalVideoClient;
        YouTubeUploader.Client = originalUploaderClient;
        FileInformation.Client = originalFileInfoClient;
        FileStorageClient.ServerConfigs.Remove("yt_pl_test_temp");

        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void DownloadPlaylist_BreakOnExisting_StopsEarlyWhenEncounteringExistingVideoTest() {
        var uploader = new YouTubeUploader {
            Id = "@testcreator",
            Name = "Test Creator"
        };
        YouTubeUploader.Client.Set(uploader);

        var video1 = new YouTubeVideo {
            Id = "vid1",
            Title = "Video 1",
            Author = "Test Creator",
            AuthorId = "@testcreator"
        };
        var video2 = new YouTubeVideo {
            Id = "vid2",
            Title = "Video 2",
            Author = "Test Creator",
            AuthorId = "@testcreator"
        };
        var video3 = new YouTubeVideo {
            Id = "vid3",
            Title = "Video 3",
            Author = "Test Creator",
            AuthorId = "@testcreator"
        };
        YouTubeVideo.Client.Set(video1);
        YouTubeVideo.Client.Set(video2);
        YouTubeVideo.Client.Set(video3);

        var playlist = new YouTubePlaylist {
            Id = "PL_TEST",
            Title = "Test Playlist",
            Videos = ["vid1", "vid2", "vid3"]
        };
        YouTubePlaylist.Client.Set(playlist);

        // In reverse order (newest first), the iteration is vid3, vid2, vid1.
        // Register vid3 canonical file as existing in the system via FileInformation
        testFileInfoClient.AddFile($"{YoutubeCommand.RepoPath}/vid3.mp4");

        var cmd = new DownloadPlaylistCommand {
            PlaylistId = "PL_TEST",
            OutputFolder = $"{tempDir}/output",
            BreakOnExisting = true
        };

        var exitCode = cmd.Execute();
        exitCode.Should().Be(0);
        cmd.Results.Should().HaveCount(1);
        cmd.Results[0].item.Should().Be("vid3");
    }
}
