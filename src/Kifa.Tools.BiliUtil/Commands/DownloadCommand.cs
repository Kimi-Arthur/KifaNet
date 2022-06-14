using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

public abstract class DownloadCommand : KifaCommand {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [Option('d', "prefix-date", HelpText = "Prefix file name with the upload date.")]
    public bool PrefixDate { get; set; } = false;

    [Option('s', "source", HelpText = "Override default source choice.")]
    public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

    [Option('o', "output-folder",
        HelpText = "Folder to output video files to. Defaults to current folder.")]
    public string? OutputFolder { get; set; }

    [Option('a', "output-audio", HelpText = "Also generate audio file in destination.")]
    public bool OutputAudio { get; set; } = false;
    
    public DownloadOptions DownloadOptions
        => new() {
            PrefixDate = PrefixDate,
            SourceChoice = SourceChoice,
            OutputFolder = OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder
        };
    
    public static bool DownloadPart(BilibiliVideo video, int pid,
        DownloadOptions downloadOptions, string alternativeFolder = null,
        BilibiliUploader uploader = null) {
        uploader ??= new BilibiliUploader {
            Id = video.AuthorId,
            Name = video.Author
        };

        var (extension, quality, streamGetters) =
            video.GetVideoStreams(pid, downloadOptions.SourceChoice);
        if (extension == null) {
            logger.Warn("Failed to get video streams.");
            return false;
        }

        var currentFolder = downloadOptions.OutputFolder;
        var prefix =
            $"{video.GetDesiredName(pid, quality, alternativeFolder: alternativeFolder, prefixDate: downloadOptions.PrefixDate, uploader: uploader)}";
        var canonicalPrefix = video.GetCanonicalName(pid, quality);
        var canonicalTargetFile = currentFolder.GetFile($"{canonicalPrefix}.mp4");
        var finalTargetFile = currentFolder.GetFile($"{prefix}.mp4");

        if (finalTargetFile.ExistsSomewhere()) {
            logger.Info($"{finalTargetFile.FileInfo.Id} already exists in the system. Skipped.");
            if (!canonicalTargetFile.ExistsSomewhere()) {
                FileInformation.Client.Link(finalTargetFile.Id, canonicalTargetFile.Id);
                logger.Info($"Linked {canonicalTargetFile.Id} ==> {finalTargetFile.Id}");
            }

            return true;
        }

        if (finalTargetFile.Exists()) {
            logger.Info($"Target file {finalTargetFile} already exists. Skipped.");
            if (!canonicalTargetFile.Exists()) {
                finalTargetFile.Copy(canonicalTargetFile);
                logger.Info($"Linked {finalTargetFile} ==> {canonicalTargetFile}");
            }

            return true;
        }

        if (canonicalTargetFile.ExistsSomewhere()) {
            logger.Info(
                $"{canonicalTargetFile.FileInfo.Id} already exists in the system. Skipped.");

            FileInformation.Client.Link(canonicalTargetFile.Id, finalTargetFile.Id);
            logger.Info($"Linked {finalTargetFile.Id} ==> {canonicalTargetFile.Id}");

            return true;
        }

        if (canonicalTargetFile.Exists()) {
            logger.Info($"Target file {finalTargetFile} already exists. Skipped.");

            canonicalTargetFile.Copy(finalTargetFile);
            logger.Info($"Linked {canonicalTargetFile} ==> {finalTargetFile}");

            return true;
        }

        var partFiles = new List<KifaFile>();
        for (var i = 0; i < streamGetters.Count; i++) {
            var targetFile = currentFolder.GetFile($"{canonicalPrefix}-{i + 1}.{extension}");
            logger.Debug($"Writing to part file ({i + 1}): {targetFile}...");
            try {
                targetFile.Write(streamGetters[i]);
                logger.Debug($"Written to part file ({i + 1}): {targetFile}.");
            } catch (Exception e) {
                logger.Warn(e, $"Failed to download {targetFile}.");
                return false;
            }

            partFiles.Add(targetFile);
        }

        try {
            logger.Debug(
                $"Merging and removing part files ({streamGetters.Count}) to {canonicalTargetFile}...");
            Helper.MergePartFiles(partFiles, canonicalTargetFile);
            RemovePartFiles(partFiles);
            logger.Debug(
                $"Merged and removed part files ({streamGetters.Count}) to {canonicalTargetFile}.");

            logger.Debug($"Copying from {canonicalTargetFile} to {finalTargetFile}...");
            canonicalTargetFile.Copy(finalTargetFile);
            logger.Debug($"Copied from {canonicalTargetFile} to {finalTargetFile}.");
        } catch (Exception e) {
            logger.Warn(e, $"Failed to merge files.");
            return false;
        }

        return true;
        // Temporarily disable this part as it seems not applicable anymore.
        // TODO: verify and remove this logic.
        // } else {
        //     var targetFile = currentFolder.GetFile(
        //         $"{video.GetDesiredName(pid, quality, alternativeFolder: alternativeFolder, prefixDate: downloadOptions.PrefixDate)}.{extension}");
        //     logger.Debug($"Writing to {targetFile}...");
        //     try {
        //         targetFile.Write(streamGetters.First());
        //         logger.Debug($"Successfully written to {targetFile}.");
        //     } catch (Exception e) {
        //         logger.Warn(e, $"Failed to download {targetFile}.");
        //         return false;
        //     }
        //
        //     return true;
    }

    static void RemovePartFiles(List<KifaFile> partFiles) {
        partFiles.ForEach(p => p.Delete());
    }
}
