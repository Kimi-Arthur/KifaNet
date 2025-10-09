using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.Bilibili.BilibiliApi;
using NLog;

namespace Kifa.Tools.BiliUtil.Commands;

public abstract class DownloadCommand : BiliCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Option('p', "prefix",
        HelpText =
            "Prefix of file name. Possible values: date, number (only suggested for archives)")]
    public string? Prefix { get; set; }

    [Option('c', "preferred-codec",
        HelpText = "Codec preferred to download. Supported: avc, hevc, av1. Default is hevc.")]
    public string? PreferredCodec { get; set; }

    [Option('q', "max-quality", HelpText = "Max quality to download. 120: 4K.")]
    public int MaxQuality { get; set; }

    [Option('o', "output-folder",
        HelpText = "Folder to output video files to. Defaults to current folder.")]
    public string? OutputFolder { get; set; }

    [Option('t', "include-page-title",
        HelpText =
            "Whether to include page title. Possible values: OnlyMultiplePage (default), Never, Always.")]
    public PageTitleOption IncludePageTitle { get; set; } = PageTitleOption.OnlyMultiplePage;

    int downloadCounter;

    protected void Download(BilibiliVideo video, int pid, string? alternativeFolder = null,
        string? extraFolder = null, BilibiliUploader? uploader = null,
        BilibiliRegion region = BilibiliRegion.Direct) {
        string? extension;
        int quality;
        int codec;
        Func<Stream>? videoStreamGetter;
        List<Func<Stream>>? audioStreamGetters;
        try {
            (extension, quality, codec, videoStreamGetter, audioStreamGetters) = video.GetStreams(
                pid, maxQuality: MaxQuality, preferredCodec: PreferredCodec, region: region);
        } catch (BilibiliVideoNotFoundException ex) {
            Logger.Warn(ex, "Video not found. Maybe data needs to be updated.");
            video = BilibiliVideo.Client.Get(video.Id.Checked(), true).Checked();
            (extension, quality, codec, videoStreamGetter, audioStreamGetters) =
                video.GetStreams(pid, maxQuality: MaxQuality, preferredCodec: PreferredCodec);
        }

        var outputFolder = OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder;
        var includePageTitle = IncludePageTitle switch {
            PageTitleOption.Never => false,
            PageTitleOption.Always => true,
            PageTitleOption.OnlyMultiplePage => video.Pages.Count > 1,
            _ => throw new ArgumentOutOfRangeException(nameof(IncludePageTitle),
                "Unexpected PageTitleOption")
        };

        var desiredName = video.GetDesiredName(pid, quality, codec,
            includePageTitle: includePageTitle, alternativeFolder: alternativeFolder,
            extraFolder: extraFolder, prefix: GetPrefix(video), uploader: uploader);
        var desiredFile = outputFolder.GetFile($"{desiredName}.mp4");
        var targetFiles = video.GetCanonicalNames(pid, quality, codec)
            .Select(f => GetCanonicalFile(desiredFile.Host, $"{f}.mp4")).Append(desiredFile)
            .ToList();

        var found = KifaFile.FindOne(targetFiles);

        if (found != null) {
            var message = found.ExistsSomewhere()
                ? $"{found.Id} exists in the system"
                : $"{found} exists locally";
            Logger.Info($"Found {message}. Will link instead.");
            KifaFile.LinkAll(found, targetFiles);
            return;
        }

        var canonicalTargetFile = targetFiles[0];

        var coverLink = new KifaFile(video.Cover.ToString());
        var coverFile = canonicalTargetFile.Parent.GetFile(
            $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.c.{coverLink.Extension}");
        coverFile.Write(coverLink.OpenRead);

        var trackFiles = new List<KifaFile>();
        var videoFile = canonicalTargetFile.Parent.GetFile(
            $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.v.{extension}");
        Logger.Debug($"Writing video file to {videoFile}...");

        videoFile.Write(videoStreamGetter);
        trackFiles.Add(videoFile);
        Logger.Debug($"Written video file to {videoFile}...");

        for (var i = 0; i < audioStreamGetters.Count; i++) {
            var targetFile = canonicalTargetFile.Parent.GetFile(
                $"{KifaFile.DefaultIgnoredPrefix}{canonicalTargetFile.BaseName}.a{i}.{extension}");
            Logger.Debug($"Writing to audio file ({i + 1}): {targetFile}...");

            targetFile.Write(audioStreamGetters[i]);
            Logger.Debug($"Written to audio file ({i + 1}): {targetFile}.");

            trackFiles.Add(targetFile);
        }

        Logger.Debug(
            $"Merging 1 video file and {audioStreamGetters.Count} audio files to {canonicalTargetFile}...");
        canonicalTargetFile.Delete();
        MergePartFiles(trackFiles, coverFile, canonicalTargetFile);
        Logger.Debug(
            $"Merged 1 video file and {audioStreamGetters.Count} audio files to {canonicalTargetFile}.");

        foreach (var p in trackFiles) {
            p.Delete();
        }

        coverFile.Delete();
        Logger.Debug("Removed temp files.");

        KifaFile.LinkAll(canonicalTargetFile, targetFiles);
    }

    string GetPrefix(BilibiliVideo video)
        => Prefix switch {
            "date" => $"{video.Uploaded.Checked():yyyy-MM-dd}",
            "number" => $"{++downloadCounter:D2}",
            _ => ""
        };

    static void MergePartFiles(List<KifaFile> parts, KifaFile cover, KifaFile target) {
        var result = Executor.Run("ffmpeg",
            string.Join(" ", parts.Select(f => $"-i \"{f.GetLocalPath()}\"")) +
            $" -i \"{cover.GetLocalPath()}\" " +
            string.Join(" ", parts.Select((_, index) => $"-map {index}")) + " -c copy" +
            $" -map {parts.Count} -disposition:v:1 attached_pic " +
            $" \"{target.GetLocalPath()}\"");

        if (result.ExitCode != 0) {
            throw new Exception("Merging files failed.");
        }
    }
}

public enum PageTitleOption {
    OnlyMultiplePage,
    Never,
    Always
}
