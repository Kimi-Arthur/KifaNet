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

    [Option('c', "include-cover", HelpText = "Output cover file alongside with the video.")]
    public bool IncludeCover { get; set; } = false;

    public bool Download(BilibiliVideo video, int pid, string alternativeFolder = null,
        BilibiliUploader uploader = null) {
        var outputFiles = DownloadVideo(video, pid, alternativeFolder, uploader);
        if (outputFiles == null) {
            return false;
        }

        if (IncludeCover) {
            DownloadCover(video.Cover, outputFiles);
        }

        return !OutputAudio || ExtractAudioFiles(outputFiles);
    }

    void DownloadCover(Uri videoCover, List<KifaFile> outputFiles) {
        var ext = videoCover.AbsolutePath.Split(".").Last();
        var targetFiles = outputFiles.Select(f => f.Parent.GetFile($"{f.BaseName}.{ext}")).ToList();
        // TODO: Merge with the implementation as of ExtractAudio.
    }

    public List<KifaFile>? DownloadVideo(BilibiliVideo video, int pid,
        string alternativeFolder = null, BilibiliUploader uploader = null) {
        uploader ??= new BilibiliUploader {
            Id = video.AuthorId,
            Name = video.Author
        };

        var (extension, quality, streamGetters) = video.GetVideoStreams(pid, SourceChoice);
        if (extension == null) {
            logger.Warn("Failed to get video streams.");
            return null;
        }

        var outputFolder = OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder;
        var prefix =
            $"{video.GetDesiredName(pid, quality, alternativeFolder: alternativeFolder, prefixDate: PrefixDate, uploader: uploader)}";
        var canonicalPrefix = video.GetCanonicalName(pid, quality);
        var canonicalTargetFile = outputFolder.GetFile($"{canonicalPrefix}.mp4");
        var finalTargetFile = outputFolder.GetFile($"{prefix}.mp4");

        if (finalTargetFile.ExistsSomewhere()) {
            logger.Info($"{finalTargetFile.FileInfo.Id} already exists in the system. Skipped.");
            if (!canonicalTargetFile.ExistsSomewhere()) {
                FileInformation.Client.Link(finalTargetFile.Id, canonicalTargetFile.Id);
                logger.Info($"Linked {canonicalTargetFile.Id} ==> {finalTargetFile.Id}");
            }

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        if (finalTargetFile.Exists()) {
            logger.Info($"Target file {finalTargetFile} already exists. Skipped.");
            if (!canonicalTargetFile.Exists()) {
                finalTargetFile.Copy(canonicalTargetFile);
                logger.Info($"Linked {finalTargetFile} ==> {canonicalTargetFile}");
            }

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        if (canonicalTargetFile.ExistsSomewhere()) {
            logger.Info(
                $"{canonicalTargetFile.FileInfo.Id} already exists in the system. Skipped.");

            FileInformation.Client.Link(canonicalTargetFile.Id, finalTargetFile.Id);
            logger.Info($"Linked {finalTargetFile.Id} ==> {canonicalTargetFile.Id}");

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        if (canonicalTargetFile.Exists()) {
            logger.Info($"Target file {finalTargetFile} already exists. Skipped.");

            canonicalTargetFile.Copy(finalTargetFile);
            logger.Info($"Linked {canonicalTargetFile} ==> {finalTargetFile}");

            return new List<KifaFile> {
                canonicalTargetFile,
                finalTargetFile
            };
        }

        var partFiles = new List<KifaFile>();
        for (var i = 0; i < streamGetters.Count; i++) {
            var targetFile = outputFolder.GetFile($"{canonicalPrefix}-{i + 1}.{extension}");
            logger.Debug($"Writing to part file ({i + 1}): {targetFile}...");
            try {
                targetFile.Write(streamGetters[i]);
                logger.Debug($"Written to part file ({i + 1}): {targetFile}.");
            } catch (Exception e) {
                logger.Warn(e, $"Failed to download {targetFile}.");
                return null;
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

    bool ExtractAudioFiles(List<KifaFile> outputFiles) {
        var targetFiles = outputFiles.Select(f => f.Parent.GetFile(f.BaseName + ".m4a")).ToList();
        var existingTargetFile = targetFiles.FirstOrDefault(f => f.ExistsSomewhere() || f.Exists());
        if (existingTargetFile != null) {
            if (existingTargetFile.ExistsSomewhere()) {
                foreach (var file in targetFiles) {
                    if (file != existingTargetFile) {
                        FileInformation.Client.Link(existingTargetFile.Id, file.Id);
                        logger.Info($"Linked {existingTargetFile.Id} ==> {file.Id}.");
                    }
                }
            } else {
                foreach (var file in targetFiles) {
                    if (file != existingTargetFile) {
                        existingTargetFile.Copy(file);
                        logger.Info($"Linked {existingTargetFile} ==> {file}.");
                    }
                }
            }
        } else {
            logger.Info("Getting video files to local for transform...");
            // TODO: Skipped for now. Assuming the first file exists.

            logger.Info($"Extracting audio files to {targetFiles[0]}...");
            Helper.ExtractAudioFile(outputFiles[0], targetFiles[0]);
            logger.Info("Extracted audio files.");
            foreach (var file in targetFiles.Skip(1)) {
                targetFiles[0].Copy(file);
                logger.Info($"Linked {targetFiles[0]} ==> {file}.");
            }
        }

        return true;
    }
}
