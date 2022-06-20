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

    [Option('c', "include-cover", HelpText = "Output cover file alongside with the video.")]
    public bool IncludeCover { get; set; } = false;

    public bool Download(BilibiliVideo video, int pid, string? alternativeFolder = null,
        BilibiliUploader? uploader = null) {
        var outputFiles = DownloadVideo(video, pid, alternativeFolder, uploader);
        if (outputFiles == null) {
            return false;
        }

        if (IncludeCover) {
            DownloadCover(video.Cover, outputFiles);
        }

        return true;
    }

    void DownloadCover(Uri videoCover, List<KifaFile> outputFiles) {
        var ext = videoCover.AbsolutePath.Split(".").Last();
        var targetFiles = outputFiles.Select(f => f.Parent.GetFile($"{f.BaseName}.{ext}")).ToList();
        // TODO: Merge with the implementation as of ExtractAudio.
    }

    public List<KifaFile>? DownloadVideo(BilibiliVideo video, int pid,
        string? alternativeFolder = null, BilibiliUploader? uploader = null) {
        uploader ??= new BilibiliUploader {
            Id = video.AuthorId,
            Name = video.Author
        };

        var (extension, quality, streamGetters) = video.GetVideoStreams(pid, SourceChoice);
        if (extension == null) {
            Logger.Warn("Failed to get video streams.");
            return null;
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

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        if (finalTargetFile.Exists()) {
            Logger.Info($"Target file {finalTargetFile} already exists. Skipped.");
            if (!canonicalTargetFile.Exists()) {
                finalTargetFile.Copy(canonicalTargetFile);
                Logger.Info($"Linked {finalTargetFile} ==> {canonicalTargetFile}");
            }

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        if (canonicalTargetFile.ExistsSomewhere()) {
            Logger.Info(
                $"{canonicalTargetFile.Id} already exists in the system. Skipped.");

            FileInformation.Client.Link(canonicalTargetFile.Id, finalTargetFile.Id);
            Logger.Info($"Linked {finalTargetFile.Id} ==> {canonicalTargetFile.Id}");

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        if (canonicalTargetFile.Exists()) {
            Logger.Info($"Target file {finalTargetFile} already exists. Skipped.");

            canonicalTargetFile.Copy(finalTargetFile);
            Logger.Info($"Linked {canonicalTargetFile} ==> {finalTargetFile}");

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        var partFiles = new List<KifaFile>();
        for (var i = 0; i < streamGetters.Count; i++) {
            var targetFile = outputFolder.GetFile($"{canonicalPrefix}-{i + 1}.{extension}");
            Logger.Debug($"Writing to part file ({i + 1}): {targetFile}...");
            try {
                targetFile.Write(streamGetters[i]);
                Logger.Debug($"Written to part file ({i + 1}): {targetFile}.");
            } catch (Exception e) {
                Logger.Warn(e, $"Failed to download {targetFile}.");
                return null;
            }

            partFiles.Add(targetFile);
        }

        try {
            Logger.Debug(
                $"Merging and removing part files ({streamGetters.Count}) to {canonicalTargetFile}...");
            Helper.MergePartFiles(partFiles, canonicalTargetFile);
            RemovePartFiles(partFiles);
            Logger.Debug(
                $"Merged and removed part files ({streamGetters.Count}) to {canonicalTargetFile}.");

            Logger.Debug($"Copying from {canonicalTargetFile} to {finalTargetFile}...");
            canonicalTargetFile.Copy(finalTargetFile);
            Logger.Debug($"Copied from {canonicalTargetFile} to {finalTargetFile}.");
        } catch (Exception e) {
            Logger.Warn(e, $"Failed to merge files.");
            return null;
        }


        return new List<KifaFile> {
            canonicalTargetFile,
            finalTargetFile
        };
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

    static void RemovePartFiles(List<KifaFile> partFiles) {
        foreach (var p in partFiles) {
            p.Delete();
        }
    }
}
