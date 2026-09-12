using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Jobs;
using Kifa.Service;
using Kifa.YouTube;
using NLog;

namespace Kifa.Tools.YoutubeUtil.Commands;

[Verb("import", HelpText = "Import YouTube video metadata from local video file(s).")]
public class ImportVideoCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target video file(s) or folder(s) to import metadata from.")]
    public IEnumerable<string> Files { get; set; } = [];

    [Option('r', "refresh", HelpText = "Force refresh existing video data before updating.")]
    public bool Refresh { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var fileList = KifaFile.FindAllFiles(Files);
        if (fileList.Count == 0) {
            Logger.Fatal("No matching files found.");
            return 1;
        }

        foreach (var file in fileList) {
            ExecuteItem(file.ToString(), () => ImportVideo(file));
        }

        return LogSummary();
    }

    class FieldUpdate {
        public string Display { get; init; } = "";
        public Action<YouTubeVideo> Apply { get; init; } = _ => { };
    }

    KifaActionResult ImportVideo(KifaFile file) {
        var localPath = file.GetLocalPath();
        if (!File.Exists(localPath)) {
            return KifaActionResult.Error($"File '{localPath}' does not exist locally.");
        }

        var imported = YouTubeVideo.FromMediaFile(localPath);
        if (imported == null || imported.Id == null) {
            return KifaActionResult.Error($"Failed to extract YouTube video metadata from '{localPath}'.");
        }

        var existing = YouTubeVideo.Client.Get(imported.Id, refresh: Refresh) ?? new YouTubeVideo {
            Id = imported.Id
        };

        var candidateUpdates = GetCandidateUpdates(existing, imported);
        if (candidateUpdates.Count == 0) {
            Logger.Info($"No new metadata or field changes to import for video {existing.Id} ({file}).");
            return KifaActionResult.Success("No metadata changes to import.");
        }

        var selected = SelectMany(
            candidateUpdates,
            choiceToString: u => u.Display,
            choiceSummaryString: new Func<List<FieldUpdate>, string>(_ => $"fields to update for video {existing.Id}"),
            selectionKey: $"import_fields_{existing.Id}"
        );

        if (selected.Status != KifaActionStatus.OK) {
            return new KifaActionResult {
                Status = selected.Status,
                Message = $"Field selection skipped or cancelled for {existing.Id}."
            };
        }

        if (selected.Value == null || selected.Value.Count == 0) {
            return KifaActionResult.Skipped($"No fields chosen to import for {existing.Id}.");
        }

        foreach (var update in selected.Value) {
            update.Apply(existing);
        }

        YouTubeVideo.Client.Set(existing);
        Logger.Info(
            $"Successfully imported {selected.Value.Count} field(s) for YouTubeVideo {existing.Id}: '{existing.Title}' by '{existing.Author}'.");
        return KifaActionResult.Success();
    }

    static List<FieldUpdate> GetCandidateUpdates(YouTubeVideo existing, YouTubeVideo imported) {
        var updates = new List<FieldUpdate>();

        if (imported.Title != null && imported.Title != existing.Title) {
            updates.Add(new FieldUpdate {
                Display = existing.Title == null
                    ? $"Title: \"{imported.Title}\""
                    : $"Title: \"{existing.Title}\" -> \"{imported.Title}\"",
                Apply = v => v.Title = imported.Title
            });
        }

        var authorChanged = imported.Author != null &&
                            (imported.Author != existing.Author ||
                             (imported.AuthorId != null && imported.AuthorId != existing.AuthorId));
        if (authorChanged) {
            var existingAuthorDisplay = existing.Author == null
                ? null
                : (existing.AuthorId != null ? $"{existing.Author} ({existing.AuthorId})" : existing.Author);
            var importedAuthorDisplay = imported.AuthorId != null
                ? $"{imported.Author} ({imported.AuthorId})"
                : imported.Author;

            updates.Add(new FieldUpdate {
                Display = existingAuthorDisplay == null
                    ? $"Author: {importedAuthorDisplay}"
                    : $"Author: {existingAuthorDisplay} -> {importedAuthorDisplay}",
                Apply = v => {
                    v.Author = imported.Author;
                    if (imported.AuthorId != null) {
                        v.AuthorId = imported.AuthorId;
                    }
                }
            });
        }

        if (imported.Description != null && imported.Description != existing.Description) {
            var existingDesc = existing.Description == null ? null : FormatDescription(existing.Description);
            var importedDesc = FormatDescription(imported.Description);
            updates.Add(new FieldUpdate {
                Display = existingDesc == null
                    ? $"Description: \"{importedDesc}\""
                    : $"Description: \"{existingDesc}\" -> \"{importedDesc}\"",
                Apply = v => v.Description = imported.Description
            });
        }

        if (imported.UploadDate != null && imported.UploadDate != existing.UploadDate) {
            updates.Add(new FieldUpdate {
                Display = existing.UploadDate == null
                    ? $"UploadDate: {imported.UploadDate}"
                    : $"UploadDate: {existing.UploadDate} -> {imported.UploadDate}",
                Apply = v => v.UploadDate = imported.UploadDate
            });
        }

        if (imported.Duration != TimeSpan.Zero && imported.Duration != existing.Duration) {
            updates.Add(new FieldUpdate {
                Display = existing.Duration == TimeSpan.Zero
                    ? $"Duration: {imported.Duration}"
                    : $"Duration: {existing.Duration} -> {imported.Duration}",
                Apply = v => v.Duration = imported.Duration
            });
        }

        return updates;
    }

    static string FormatDescription(string description, int maxLength = 60) {
        var singleLine = description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
        return singleLine.Length > maxLength ? $"{singleLine[..maxLength]}..." : singleLine;
    }
}
