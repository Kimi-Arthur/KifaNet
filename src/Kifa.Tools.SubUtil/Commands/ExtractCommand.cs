using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using FFMpegCore;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.SubUtil.Commands;

[Verb("extract", HelpText = "Extract subtitle from video files.")]
class ExtractCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly Dictionary<string, string> SubtitleExtensions = new() {
        { "subrip", "srt" },
        { "ass", "ass" },
        { "hdmv_pgs_subtitle", "sup" }
    };

    [Option('g', "group", HelpText = "Group name for the source, like SMURF, 人人影视.")]
    public string? Group { get; set; }

    [Option('f', "force", HelpText = "Forcing extracting the subtitle.")]
    public bool Force { get; set; }

    [Value(0, Required = true, HelpText = "Files to combine.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var foundFiles = KifaFile.FindExistingFiles(FileNames);
        foundFiles = foundFiles.Where(f => !Common.SubtitleExtensions.Contains(f.Extension))
            .ToList();

        foreach (var file in foundFiles) {
            Console.WriteLine(file);
        }

        if (!Confirm(
                $"Confirm extracting subtitles of the {foundFiles.Count} above and place in relevant folders in Subtitles cell?")) {
            Logger.Info("Gave up extrating. Exiting.");
            return 0;
        }

        foreach (var file in foundFiles) {
            var info = FFProbe.Analyse(file.GetLocalPath());
            var selected = SelectOne(info.SubtitleStreams,
                s => $"{ExtractLanguage(s.Language)} ({s.Language})" +
                     (s.Tags?.ContainsKey("title") ?? false ? $": {s.Tags["title"]}" : "") +
                     $" => {GetExtractedSubtitleFile(file, s)}");

            ExecuteItem(file.ToString(), () => ExtractSubtitle(file, selected.Value.Choice));
        }

        return LogSummary();
    }

    KifaActionResult ExtractSubtitle(KifaFile file, SubtitleStream choice) {
        var subtitleFile = GetExtractedSubtitleFile(file, choice);
        if (subtitleFile.Exists()) {
            if (!Force) {
                return new KifaActionResult {
                    Status = KifaActionStatus.OK,
                    Message = $"Subtitle file {subtitleFile} already exists. Skipped"
                };
            }

            Logger.Info(
                $"Though subtitle file {subtitleFile} exists. It's requested to overwritten.");
        }

        subtitleFile.EnsureLocalParent();
        subtitleFile.Delete();
        var result = Executor.Run("ffmpeg",
            $"-i \"{file.GetLocalPath()}\" -map 0:{choice.Index} -c copy \"{subtitleFile.GetLocalPath()}\"");
        if (result.ExitCode != 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message = result.ToString()
            };
        }

        return new KifaActionResult {
            Status = KifaActionStatus.OK,
            Message = $"Successfully created {subtitleFile}"
        };
    }

    KifaFile GetExtractedSubtitleFile(KifaFile file, SubtitleStream subtitle)
        => file.GetSubtitleFile(ExtractLanguage(subtitle.Language).Code +
                                string.Or($"-{Group}.", ".") +
                                SubtitleExtensions[subtitle.CodecName]);

    static Language ExtractLanguage(string? languageName)
        => languageName == null ? Language.Unknown :
            languageName.Contains('-') ? languageName[..languageName.IndexOf("-")] : languageName;
}
