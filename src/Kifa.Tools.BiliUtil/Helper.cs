using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kifa.Api.Files;
using NLog;

namespace Kifa.Tools.BiliUtil;

public static class Helper {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void MergePartFiles(List<KifaFile> parts, KifaFile target) {
        // Convert parts first
        var partFiles = parts.Select(ConvertPartFile).ToList();

        var fileListFile = target.Parent.GetFile($"!{target.BaseName}.list");
        fileListFile.Write(string.Join("\n",
            partFiles.Select(p => $"file {GetFfmpegTargetPath(p.GetLocalPath())}")));

        var arguments =
            $"-safe 0 -f concat -i \"{fileListFile.GetLocalPath()}\" -c copy \"{target.GetLocalPath()}\"";
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

        fileListFile.Delete();

        // Delete part files
        foreach (var partFile in partFiles) {
            partFile.Delete();
        }
    }

    static KifaFile ConvertPartFile(KifaFile inputFile) {
        var newPath = inputFile.Parent.GetFile($"!{inputFile.BaseName}.mp4");
        var arguments = $"-i \"{inputFile.GetLocalPath()}\" -c copy \"{newPath.GetLocalPath()}\"";
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
