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

        var subfolderAids = SkipSubfolders ? GetSubfolderAids(uploader) : null;
        if (subfolderAids != null) {
            Logger.Info($"Found {subfolderAids.Count} videos in subfolders to skip.");
            Logger.Debug($"Videos to skip: {string.Join(", ", subfolderAids)}");
        }

        var aids = OldestFirst ? uploader.Aids : Enumerable.Reverse(uploader.Aids).ToList();
        if (subfolderAids != null) {
            aids = aids.Where(aid => !subfolderAids.Contains(aid)).ToList();
        }

        var videos = SelectMany(BilibiliVideo.Client.Get(aids).ExceptNull().ToList(),
            video => $"{video.Id} {video.Title} ({video.Pages.Count})", "videos to download");
        if (videos.Status == KifaActionStatus.OK) {
            DownloadVideos(uploader, videos.Value);
        }

        var removedAids = OldestFirst
            ? uploader.RemovedAids
            : Enumerable.Reverse(uploader.RemovedAids).ToList();
        if (subfolderAids != null) {
            removedAids = removedAids.Where(aid => !subfolderAids.Contains(aid)).ToList();
        }

        var deletedVideos = SelectMany(BilibiliVideo.Client.Get(removedAids).ExceptNull().ToList(),
            video => $"{video.Id} {video.Title} ({video.Pages.Count})",
            "deleted videos to download");
        if (deletedVideos.Status == KifaActionStatus.OK) {
            DownloadVideos(uploader, deletedVideos.Value);
        }

        return LogSummary();
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
        }
    }

    KifaActionResult DownloadVideo(BilibiliUploader uploader, BilibiliVideo video) {
        var results = new KifaBatchActionResult();
        foreach (var page in video.Pages) {
            results.Add($"{video.Id}p{page.Id} {video.Title} {page.Title}",
                KifaActionResult.FromAction(()
                    => Download(video, page.Id, uploader: uploader, extraFolder: InnerFolder)));
        }

        return results;
    }
}
