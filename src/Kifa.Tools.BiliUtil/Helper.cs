using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Api.Files;
using Kifa.Bilibili;
using NLog;

namespace Kifa.Tools.BiliUtil;

static class Helper {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void MergePartFiles(List<KifaFile> parts, KifaFile cover, KifaFile target) {
        var result = Executor.Run("ffmpeg",
            string.Join(" ", parts.Select(f => $"-i \"{f.GetLocalPath()}\"")) +
            $" -i \"{cover.GetLocalPath()}\" -map {parts.Count} -disposition:v:0 attached_pic " +
            string.Join(" ", parts.Select((_, index) => $"-map {index}")) +
            $" -c copy \"{target.GetLocalPath()}\"");

        if (result.ExitCode != 0) {
            throw new Exception("Merging files failed.");
        }
    }

    static readonly Regex FileNamePattern = new Regex(@"-(av\d+)(p\d+)?\.(c\d+)\.(\d+).mp4");

    public static string? InferAid(string file) {
        var segments = file.Substring(file.LastIndexOf('-') + 1).Split('.');
        if (segments.Length < 3 || !segments[segments.Length - 3].StartsWith("av")) {
            Logger.Debug("Cannot infer CID from file name.");
            return null;
        }

        return segments[segments.Length - 3];
    }


    public static (BilibiliVideo? video, int pid, int quality) GetVideo(string file) {
        var match = FileNamePattern.Match(file);
        if (!match.Success) {
            return (null, 1, 0);
        }

        return (BilibiliVideo.Client.Get(match.Groups[1].Value),
            match.Groups[2].Success ? int.Parse(match.Groups[2].Value[1..]) : 1,
            match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0);
    }
}
