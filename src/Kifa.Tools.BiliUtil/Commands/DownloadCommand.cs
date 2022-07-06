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

    [Option('s', "source", HelpText = "Override default source choice.")]
    public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

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

        var (extension, quality, streamGetters) = video.GetVideoStreams(pid, SourceChoice);
        if (extension == null) {
            Logger.Warn("Failed to get video streams.");
            return 1;
        }

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

        var partFiles = new List<KifaFile>();
        for (var i = 0; i < streamGetters.Count; i++) {
            var targetFile =
                canonicalTargetFile.Parent.GetFile($"!{canonicalTargetFile.Name}.{extension}");
            Logger.Debug($"Writing to part file ({i + 1}): {targetFile}...");
            try {
                targetFile.Write(streamGetters[i]);
                Logger.Debug($"Written to part file ({i + 1}): {targetFile}.");
            } catch (Exception e) {
                Logger.Warn(e, $"Failed to download {targetFile}.");
                return 1;
            }

            partFiles.Add(targetFile);
        }

        var coverLink = new KifaFile(video.Cover.ToString());
        var coverFile =
            canonicalTargetFile.Parent.GetFile(
                $"!{canonicalTargetFile.Name}.{coverLink.Extension}");
        coverLink.Copy(coverFile);

        try {
            Logger.Debug(
                $"Merging and removing part files ({streamGetters.Count}) to {canonicalTargetFile}...");
            Helper.MergePartFiles(partFiles, coverFile, canonicalTargetFile);

            foreach (var p in partFiles) {
                p.Delete();
            }

            coverFile.Delete();

            Logger.Debug(
                $"Merged and removed part files ({streamGetters.Count}) to {canonicalTargetFile}.");

            Logger.Debug($"Copying from {canonicalTargetFile} to {finalTargetFile}...");
            canonicalTargetFile.Copy(finalTargetFile);
            Logger.Debug($"Copied from {canonicalTargetFile} to {finalTargetFile}.");
        } catch (Exception e) {
            Logger.Warn(e, "Failed to merge files.");
            return 1;
        }


        return 0;
        // Temporarily disable this part as it seems not applicable anymore.
        // TODO: verify and remove this logic.
        // } else {
        //     var targetFile = currentFolder.GetFile(
        //         $"{video.GetDesiredName(pid, quality, alternativeFolder: alternativeFolder, prefixDate: downloadOptions.PrefixDate)}.{extension}");
        //     Logger.Debug($"Writing to {targetFile}...");
        //     try {
        //         targetFile.Write(streamGetters.First());
        //         Logger.Debug($"Successfully written to {targetFile}.");
        //     } catch (Exception e) {
        //         Logger.Warn(e, $"Failed to download {targetFile}.");
        //         return false;
        //     }
        //
        //     return true;
    }
}
