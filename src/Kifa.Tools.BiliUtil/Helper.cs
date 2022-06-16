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

public static class Helper {
    static readonly Logger logger = LogManager.GetCurrentClassLogger();

    static readonly Regex fileNamePattern = new(@"^AV(\d+) P(\d+) .* cid (\d+)$");

    public static (string? aid, int pid, string? cid) GetIds(string name) {
        var match = fileNamePattern.Match(name);
        if (!match.Success) {
            return (null, 0, null);
        }

        return ($"av{match.Groups[1].Value}", int.Parse(match.Groups[2].Value),
            match.Groups[3].Value);
    }

    // TODO: move to BilibiliVideo.
    public static string? GetDesiredFileName(BilibiliVideo? video, int pid, string? cid = null) {
        if (video == null) {
            return null;
        }
        var p = video.Pages.First(x => x.Id == pid);

        if (cid != null && cid != p.Cid) {
            return null;
        }

        return video.Pages.Count > 1
            ? $"{video.Author}-{video.AuthorId}/{video.Title} P{pid} {p.Title}-{video.Id}p{pid}.c{cid}"
            : $"{video.Author}-{video.AuthorId}/{video.Title} {p.Title}-{video.Id}.c{cid}";
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

    public static string ExtractAudioFile(KifaFile sourceVideoFile, KifaFile targetAudioFile) {
        var sourcePath = ((FileStorageClient) sourceVideoFile.Client).GetPath(sourceVideoFile.Path);
        var targetPath = ((FileStorageClient) targetAudioFile.Client).GetPath(targetAudioFile.Path);

        var arguments = $"-i \"{sourcePath}\" -map 0:a -acodec copy \"{targetPath}\"";
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
            throw new Exception("Extract audio file failed.");
        }

        return targetPath;
    }

    static string GeFfmpegTargetPath(string targetPath) {
        return string.Join("\\'", targetPath.Split("'").Select(s => $"'{s}'"));
    }
}
