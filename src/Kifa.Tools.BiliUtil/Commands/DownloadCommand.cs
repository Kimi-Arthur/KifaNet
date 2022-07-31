using System;
using System.Collections.Generic;
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

        var (extension, quality, videoStreamGetter, audioStreamGetters) = video.GetStreams(pid);

        var outputFolder = OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder;
        var prefix =
            $"{video.GetDesiredName(pid, quality, alternativeFolder: alternativeFolder, prefixDate: PrefixDate, uploader: uploader)}";
        var canonicalPrefix = video.GetCanonicalName(pid, quality);
        var canonicalTargetFile = outputFolder.GetFile($"{canonicalPrefix}.mp4");
        var finalTargetFile = outputFolder.GetFile($"{prefix}.mp4");

        if (finalTargetFile.ExistsSomewhere()) {
            Logger.Info($"{finalTargetFile.Id} already exists in the system. Skipped.");
            if (!canonicalTargetFile.ExistsSomewhere()) {
                FileInformation.Client.Link(finalTargetFile.Id, canonicalTargetFile.Id);
                Logger.Info($"Linked {canonicalTargetFile.Id} ==> {finalTargetFile.Id}");
            }

            return 0;
        }

        if (finalTargetFile.Exists()) {
            Logger.Info($"Target file {finalTargetFile} already exists. Skipped.");
            if (!canonicalTargetFile.Exists()) {
                finalTargetFile.Copy(canonicalTargetFile);
                Logger.Info($"Linked {finalTargetFile} ==> {canonicalTargetFile}");
            }

            return 0;
        }

        if (canonicalTargetFile.ExistsSomewhere()) {
            Logger.Info($"{canonicalTargetFile.Id} already exists in the system. Skipped.");

            FileInformation.Client.Link(canonicalTargetFile.Id, finalTargetFile.Id);
            Logger.Info($"Linked {finalTargetFile.Id} ==> {canonicalTargetFile.Id}");

            return 0;
        }

        if (canonicalTargetFile.Exists()) {
            Logger.Info($"Target file {finalTargetFile} already exists. Skipped.");

            canonicalTargetFile.Copy(finalTargetFile);
            Logger.Info($"Linked {canonicalTargetFile} ==> {finalTargetFile}");

            return 0;
        }

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
            Helper.MergePartFiles(trackFiles, coverFile, canonicalTargetFile);
            Logger.Debug(
                $"Merged 1 video file and {audioStreamGetters.Count} audio files to {canonicalTargetFile}.");

            foreach (var p in trackFiles) {
                p.Delete();
            }

            coverFile.Delete();
            Logger.Debug("Removed temp files.");

            canonicalTargetFile.Copy(finalTargetFile);
            Logger.Debug($"Copied from {canonicalTargetFile} to {finalTargetFile}.");
        } catch (Exception e) {
            Logger.Warn(e, "Failed to merge files.");
            return 1;
        }

        return 0;
    }
}
