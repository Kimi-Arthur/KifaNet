using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

[Verb("up", HelpText = "Download all high quality Bilibili videos for one uploader.")]
public class DownloadUploaderCommand : DownloadCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Uploader ID.")]
    public string UploaderId { get; set; }

    [Option('g', "category", HelpText = "Category of the videos to download.")]
    public string? Category { get; set; }

    [Option('f', "folder",
        HelpText =
            "Extra inner folder name for the group of videos (especially if subset of videos are selected).")]
    public string? InnerFolder { get; set; }

    [Option('l', "oldest-first", HelpText = "Download oldest video first.")]
    public bool OldestFirst { get; set; } = false;

    [Option('s', "skip-subfolders", HelpText = "Skip videos found in its subfolders.")]
    public bool SkipSubfolders { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var uploader = BilibiliUploader.Client.Get(UploaderId);
        if (uploader == null) {
            Logger.Fatal($"Cannot find uploader ({UploaderId}). Exiting.");
            return 1;
        }

        var uploaderVideos = BilibiliUploaderVideos.Client.Get(uploader.Id);
        if (uploaderVideos == null) {
            Logger.Fatal($"Cannot find video list for uploader ({UploaderId}). Exiting.");
            return 1;
        }

        var subfolderAids = SkipSubfolders ? GetSubfolderAids(uploader) : null;
        if (subfolderAids != null) {
            Logger.Info($"Found {subfolderAids.Count} videos in subfolders to skip.");
            Logger.Debug($"Videos to skip: {string.Join(", ", subfolderAids)}");
        }

        var aids = OldestFirst ? uploaderVideos.Aids : Enumerable.Reverse(uploaderVideos.Aids).ToList();
        if (subfolderAids != null) {
            aids = aids.Where(aid => !subfolderAids.Contains(aid)).ToList();
        }

        var removedAids = OldestFirst
            ? uploaderVideos.RemovedAids
            : Enumerable.Reverse(uploaderVideos.RemovedAids).ToList();
        if (subfolderAids != null) {
            removedAids = removedAids.Where(aid => !subfolderAids.Contains(aid)).ToList();
        }

        var allVideos = BilibiliVideo.Client.Get(aids).ExceptNull()
            .Select(v => (Video: v, IsDeleted: false))
            .Concat(BilibiliVideo.Client.Get(removedAids).ExceptNull()
                .Select(v => (Video: v, IsDeleted: true)))
            .ToList();

        if (Category != null) {
            allVideos = allVideos
                .Where(v => string.Equals(v.Video.Category, Category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var summary = Category != null ? $"{Category} videos to download" : "videos to download";
        var videos = SelectMany(allVideos, FormatVideoChoice, summary);
        if (videos.Status == KifaActionStatus.OK && videos.Value != null) {
            DownloadVideos(uploader, videos.Value.Select(v => v.Video).ToList());
        }

        return LogSummary();
    }

    static string FormatVideoChoice((BilibiliVideo Video, bool IsDeleted) item) {
        var (video, isDeleted) = item;
        var categoryPart = string.IsNullOrEmpty(video.Category) ? "" : $"[{video.Category}]\t";
        var datePart = video.Uploaded != null ? $"{video.Uploaded:yyyy-MM-dd} " : "";
        var deletedPart = isDeleted ? " [DELETED]" : "";
        return $"{categoryPart}{datePart}{video.Id}{deletedPart} {video.Title} ({video.Pages.Count})";
    }

    public KifaFile GetUploaderFolder(BilibiliUploader uploader)
        => BaseFolder.GetFile(uploader.GetUploaderFolder());

    public HashSet<string> GetSubfolderAids(BilibiliUploader uploader) {
        var uploaderFolder = GetUploaderFolder(uploader);
        var allFiles = KifaFile.FindAllFiles([uploaderFolder.ToString()], recursive: true,
            pattern: "*.mp4");

        var folderPath = uploaderFolder.Path.TrimEnd('/');
        var targetFolderPath = InnerFolder != null
            ? uploaderFolder.GetFile(InnerFolder).Path.TrimEnd('/')
            : folderPath;

        var aids = new HashSet<string>();
        foreach (var file in allFiles) {
            if (file.Path.StartsWith($"{folderPath}/") &&
                file.ParentPath.TrimEnd('/') != folderPath &&
                file.ParentPath.TrimEnd('/') != targetFolderPath) {
                var aid = BilibiliVideo.ParseParts(file.Path).Aid;
                if (aid != null) {
                    aids.Add(aid);
                }
            }
        }

        return aids;
    }

    void DownloadVideos(BilibiliUploader uploader, List<BilibiliVideo> videos) {
        foreach (var video in videos) {
            ExecuteItem(video.Id, () => DownloadVideo(uploader, video));
            if (BreakOnExisting && LastItemAlreadyExists) {
                Logger.Info($"Stopping early: video ({video.Id}) already exists.");
                break;
            }
        }
    }

    KifaActionResult DownloadVideo(BilibiliUploader uploader, BilibiliVideo video) {
        if (video.Pages == null || video.Pages.Count == 0) {
            LastItemAlreadyExists = false;
            return KifaActionResult.Error($"No pages found for video ({video.Id}).");
        }

        var allPagesExisted = true;
        var results = new KifaBatchActionResult();
        foreach (var page in video.Pages) {
            results.Add($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                KifaActionResult.FromAction(() => {
                    var result = Download(video, page.Id, uploader: uploader,
                        extraFolder: InnerFolder);
                    if (!LastItemAlreadyExists) {
                        allPagesExisted = false;
                    }

                    return result;
                }));
        }

        LastItemAlreadyExists = allPagesExisted && video.Pages.Count > 0;
        return results;
    }
}
