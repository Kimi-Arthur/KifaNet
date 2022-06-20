using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Kifa.Api.Files;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.BiliUtil;

public static class Helper {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void MergePartFiles(List<KifaFile> parts, KifaFile target) {
        // Convert parts first
        var partPaths = parts
            .Select(p => ConvertPartFile(p.GetLocalPath())).ToList();

        var fileListPath = Path.GetTempFileName();
        File.WriteAllLines(fileListPath, partPaths.Select(p => $"file {GetFfmpegTargetPath(p)}"));

        var targetPath = target.GetLocalPath();
        var arguments = $"-safe 0 -f concat -i \"{fileListPath}\" -c copy \"{targetPath}\"";
        Logger.Debug($"Executing: ffmpeg {arguments}");
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
        Logger.Debug($"Executing: ffmpeg {arguments}");
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

    static string GetFfmpegTargetPath(string targetPath) {
        return string.Join("\\'", targetPath.Split("'").Select(s => $"'{s}'"));
    }
}
