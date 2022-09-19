using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

public abstract class DownloadCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('d', "prefix-date", HelpText = "Prefix file name with the upload date.")]
    public bool PrefixDate { get; set; } = false;

    [Option('o', "output-folder",
        HelpText = "Folder to output video files to. Defaults to current folder.")]
    public string? OutputFolder { get; set; }

    public bool Download(BilibiliVideo video, int pid, string? alternativeFolder = null,
        BilibiliUploader? uploader = null) {
        var outputFiles = DownloadVideo(video, pid, alternativeFolder, uploader);
        if (outputFiles == null) {
            return false;
        }

        return true;
    }

    public int DownloadVideo(BilibiliVideo video, int pid, string? alternativeFolder = null,
        BilibiliUploader? uploader = null) {
        uploader ??= new BilibiliUploader {
            Id = video.AuthorId,
            Name = video.Author
        };

        var (extension, quality, codec, videoStreamGetter, audioStreamGetters) = video.GetStreams(pid);

        var outputFolder = OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder;
        var desiredName =
            $"{video.GetDesiredName(pid, quality, codec, alternativeFolder: alternativeFolder, prefixDate: PrefixDate, uploader: uploader)}";
        var canonicalNames = video.GetCanonicalNames(pid, quality, codec);

        var targetFiles = canonicalNames.Append(desiredName)
            .Select(name => outputFolder.GetFile($"{name}.mp4")).ToList();
        var found = KifaFile.FindOne(targetFiles);

        if (found != null) {
            var message = found.ExistsSomewhere()
                ? $"{found.Id} exists in the system"
                : $"{found} exists locally";
            Logger.Info($"Found {message}. Will link instead.");
            KifaFile.LinkAll(found, targetFiles);
            return 0;
        }

        var canonicalTargetFile = targetFiles[0];

        var coverLink = new KifaFile(video.Cover.ToString());
        var coverFile = canonicalTargetFile.Parent.GetFile(
            $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.c.{coverLink.Extension}");
        coverLink.Copy(coverFile);

        var trackFiles = new List<KifaFile>();
        var videoFile = canonicalTargetFile.Parent.GetFile(
            $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.v.{extension}");
        Logger.Debug($"Writing video file to {videoFile}...");
        try {
            videoFile.Write(videoStreamGetter);
            trackFiles.Add(videoFile);
            Logger.Debug($"Written video file to {videoFile}...");
        } catch (Exception e) {
            Logger.Warn(e, $"Failed to download {videoFile}.");
            return 1;
        }

        for (var i = 0; i < audioStreamGetters.Count; i++) {
            var targetFile = canonicalTargetFile.Parent.GetFile(
                $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.a{i}.{extension}");
            Logger.Debug($"Writing to audio file ({i + 1}): {targetFile}...");
            try {
                targetFile.Write(audioStreamGetters[i]);
                Logger.Debug($"Written to audio file ({i + 1}): {targetFile}.");
            } catch (Exception e) {
                Logger.Warn(e, $"Failed to download {targetFile}.");
                return 1;
            }

            trackFiles.Add(targetFile);
        }

        try {
            Logger.Debug(
                $"Merging 1 video file and {audioStreamGetters.Count} audio files to {canonicalTargetFile}...");
            canonicalTargetFile.Delete();
            MergePartFiles(trackFiles, coverFile, canonicalTargetFile);
            Logger.Debug(
                $"Merged 1 video file and {audioStreamGetters.Count} audio files to {canonicalTargetFile}.");

            foreach (var p in trackFiles) {
                p.Delete();
            }

            coverFile.Delete();
            Logger.Debug("Removed temp files.");

            KifaFile.LinkAll(canonicalTargetFile, targetFiles);
        } catch (Exception e) {
            Logger.Warn(e, "Failed to merge files.");
            return 1;
        }

        return 0;
    }

    static void MergePartFiles(List<KifaFile> parts, KifaFile cover, KifaFile target) {
        var result = Executor.Run("ffmpeg",
            string.Join(" ", parts.Select(f => $"-i \"{f.GetLocalPath()}\"")) +
            $" -i \"{cover.GetLocalPath()}\" " +
            string.Join(" ", parts.Select((_, index) => $"-map {index}")) + " -c copy" +
            $" -map {parts.Count} -disposition:v:1 attached_pic " +
            $" \"{target.GetLocalPath()}\"");

        if (result.ExitCode != 0) {
            throw new Exception("Merging files failed.");
        }
    }
}
