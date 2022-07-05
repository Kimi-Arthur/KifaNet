using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Api.Files;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil;

static class Helper {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void MergePartFiles(List<KifaFile> parts, KifaFile cover, KifaFile target) {
        // Convert parts first
        var partFiles = parts.Select(ConvertPartFile).ToList();

        var fileListFile = target.Parent.GetFile($"!{target.BaseName}.list");
        fileListFile.Write(string.Join("\n",
            partFiles.Select(p => $"file {GetFfmpegTargetPath(p.GetLocalPath())}")));

        var arguments =
            $"-safe 0 -f concat -i \"{fileListFile.GetLocalPath()}\" -i \"{cover.GetLocalPath()}\" -map 1 -disposition:v:0 attached_pic -map 0 -c copy \"{target.GetLocalPath()}\"";
        Logger.Trace($"Executing: ffmpeg {arguments}");
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
        Logger.Trace($"Executing: ffmpeg {arguments}");
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

    static readonly Regex FileNamePattern = new Regex(@"-(av\d+)(p\d+)?\.(c\d+)\.");

    public static string? InferAid(string file) {
        var segments = file.Substring(file.LastIndexOf('-') + 1).Split('.');
        if (segments.Length < 3 || !segments[segments.Length - 3].StartsWith("av")) {
            Logger.Debug("Cannot infer CID from file name.");
            return null;
        }

        return segments[segments.Length - 3];
    }


    public static BilibiliVideo? GetVideo(string file) {
        var match = FileNamePattern.Match(file);
        if (!match.Success) {
            return null;
        }

        return BilibiliVideo.Client.Get(match.Groups[1].Value);
    }
}
