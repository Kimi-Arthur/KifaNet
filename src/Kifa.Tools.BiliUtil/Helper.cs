using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.BiliUtil;

public class DownloadOptions {
    public int SourceChoice { get; set; }
    public KifaFile OutputFolder { get; set; }
    public bool PrefixDate { get; set; }
}

public static class Helper {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    static readonly Regex fileNamePattern = new(@"^AV(\d+) P(\d+) .* cid (\d+)$");

    public static (string aid, int pid, string cid) GetIds(string name) {
        var match = fileNamePattern.Match(name);
        if (!match.Success) {
            return (null, 0, null);
        }

        return ($"av{match.Groups[1].Value}", int.Parse(match.Groups[2].Value),
            match.Groups[3].Value);
    }

    public static string GetDesiredFileName(BilibiliVideo video, int pid, string cid = null) {
        var p = video.Pages.First(x => x.Id == pid);

        if (cid != null && cid != p.Cid) {
            return null;
        }

        return video.Pages.Count > 1
            ? $"{video.Author}-{video.AuthorId}/{video.Title} P{pid} {p.Title}-{video.Id}p{pid}.c{cid}"
            : $"{video.Author}-{video.AuthorId}/{video.Title} {p.Title}-{video.Id}.c{cid}";
    }

    public static bool DownloadPart(this BilibiliVideo video, int pid,
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
        if (extension != "mp4") {
            var prefix =
                $"{video.GetDesiredName(pid, quality, alternativeFolder: alternativeFolder, prefixDate: downloadOptions.PrefixDate, uploader: uploader)}";
            var canonicalPrefix = video.GetCanonicalName(pid, quality);
            var canonicalTargetFile = currentFolder.GetFile($"{canonicalPrefix}.mp4");
            var finalTargetFile = currentFolder.GetFile($"{prefix}.mp4");
            if (finalTargetFile.ExistsSomewhere()) {
                logger.Info(
                    $"{finalTargetFile.FileInfo.Id} already exists in the system. Skipped.");
                if (!canonicalTargetFile.ExistsSomewhere()) {
                    FileInformation.Client.Link(finalTargetFile.Id, canonicalTargetFile.Id);
                    logger.Info($"Linked {canonicalTargetFile.Id} ==> {finalTargetFile.Id}");
                }

                return true;
            }

            if (canonicalTargetFile.ExistsSomewhere()) {
                logger.Info(
                    $"{canonicalTargetFile.FileInfo.Id} already exists in the system. Skipped.");
                if (!finalTargetFile.ExistsSomewhere()) {
                    FileInformation.Client.Link(canonicalTargetFile.Id, finalTargetFile.Id);
                    logger.Info($"Linked {finalTargetFile.Id} ==> {canonicalTargetFile.Id}");
                }

                return true;
            }

            if (canonicalTargetFile.Exists()) {
                logger.Info($"Target file {finalTargetFile} already exists. Skipped.");
                if (!finalTargetFile.Exists()) {
                    canonicalTargetFile.Copy(finalTargetFile);
                    logger.Info($"Linked {canonicalTargetFile} ==> {finalTargetFile}");
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
                MergePartFiles(partFiles, canonicalTargetFile);
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
        } else {
            var targetFile = currentFolder.GetFile(
                $"{video.GetDesiredName(pid, quality, alternativeFolder: alternativeFolder, prefixDate: downloadOptions.PrefixDate)}.{extension}");
            logger.Debug($"Writing to {targetFile}...");
            try {
                targetFile.Write(streamGetters.First());
                logger.Debug($"Successfully written to {targetFile}.");
            } catch (Exception e) {
                logger.Warn(e, $"Failed to download {targetFile}.");
                return false;
            }
        }

        return true;
    }

    public static void MergePartFiles(List<KifaFile> parts, KifaFile target) {
        // Convert parts first
        var partPaths = parts
            .Select(p => ConvertPartFile(((FileStorageClient) p.Client).GetPath(p.Path))).ToList();

        var fileListPath = Path.GetTempFileName();
        File.WriteAllLines(fileListPath, partPaths.Select(p => $"file {GeFfmpegTargetPath(p)}"));

        var targetPath = ((FileStorageClient) target.Client).GetPath(target.Path);
        var arguments = $"-safe 0 -f concat -i \"{fileListPath}\" -c copy \"{targetPath}\"";
        logger.Debug($"Executing: ffmpeg {arguments}");
        using var proc = new Process {
            StartInfo = {
                FileName = "ffmpeg",
                Arguments = arguments
            }
        };
        proc.Start();
        proc.WaitForExit();
        if (proc.ExitCode != 0) {
            throw new Exception("Merging files failed.");
        }

        File.Delete(fileListPath);

        // Delete part files
        foreach (var partPath in partPaths) {
            File.Delete(partPath);
        }
    }

    static string ConvertPartFile(string path) {
        var newPath = Path.GetTempPath() + path.Split("/").Last() + ".mp4";
        var arguments = $"-i \"{path}\" -c copy \"{newPath}\"";
        logger.Debug($"Executing: ffmpeg {arguments}");
        using var proc = new Process {
            StartInfo = {
                FileName = "ffmpeg",
                Arguments = arguments
            }
        };
        proc.Start();
        proc.WaitForExit();
        if (proc.ExitCode != 0) {
            throw new Exception("Converting part files failed.");
        }

        return newPath;
    }

    static string GeFfmpegTargetPath(string targetPath) {
        return string.Join("\\'", targetPath.Split("'").Select(s => $"'{s}'"));
    }

    static void RemovePartFiles(List<KifaFile> partFiles) {
        partFiles.ForEach(p => p.Delete());
    }
}
