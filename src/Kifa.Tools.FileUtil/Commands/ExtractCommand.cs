using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using Kifa.Service;
using NLog;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("extract", HelpText = "Extract files and add to system.")]
class ExtractCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target archive file(s) to extract.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('p', "password", HelpText = "Password for the archive.")]
    public string? Password { get; set; }

    [Option('e', "encoding",
        HelpText = "Encoding of the archive, typically for zip files with GB18030.")]
    public string? ArchiveEncoding { get; set; }

    [Option('d', "delete-source",
        HelpText = "Delete source archive files in case extraction is successful and verified.")]
    public bool DeleteSource { get; set; } = false;

    Encoding? encoding;
    Encoding Encoding => encoding ??= GetArchiveEncoding();

    [Option('s', "archive-separator",
        HelpText =
            "Separator between archive name and entry name. Default is not prepend archive name.")]
    public string? ArchiveNameSeparator { get; set; }

    [Option('S', "show-size", HelpText = "Show size for each file and total size (can be slow).")]
    public bool ShowSize { get; set; } = false;

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames);
        var selected = SelectMany(files, file => ShowSize ? $"{file} ({file.FileInfo?.Size.ToSizeString()})" : file.ToString(),
            new Func<List<KifaFile>, string>(choices
                => $"files{(ShowSize ? $" ({choices.Sum(c => c.FileInfo?.Size ?? 0).ToSizeString()})" : "")} to extract from"));

        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to extract from", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value) {
            ExecuteItem(file.ToString(), () => ExtractFile(file));
            file.Dispose();
        }

        return LogSummary();
    }

    Encoding GetArchiveEncoding() {
        if (ArchiveEncoding == null) {
            return Encoding.UTF8;
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(ArchiveEncoding);
    }

    KifaActionResult ExtractFile(KifaFile archiveFile) {
        var folder = archiveFile.Parent;
        var archive = ArchiveFactory.Open(archiveFile.GetLocalPath(), new ReaderOptions {
            Password = Password,
            ArchiveEncoding = new ArchiveEncoding(Encoding, Encoding)
        });

        var entries = archive.Entries.Where(entry => !entry.IsDirectory).Select(entry => (
            Entry: entry,
            File: folder.GetFile(ArchiveNameSeparator != null
                ? $"{archiveFile.BaseName}{ArchiveNameSeparator}{entry.Key.Checked()}"
                : entry.Key.Checked()))).Where(entry => {
            Logger.Notice(()
                => $"File:\t{entry.File} {entry.File.ExistsSomewhere()}, {entry.File.Exists()}");
            Logger.Notice(()
                => $"Expected:\tsize={entry.Entry.Size}, crc32={entry.Entry.GetCrc32InHex()}");
            Logger.Notice(()
                => $"Found:\tsize={entry.File.FileInfo?.Size}, crc32={entry.File.FileInfo?.Crc32}");

            if (entry.File.ExistsSomewhere() && entry.File.FileInfo?.Size == entry.Entry.Size &&
                entry.File.FileInfo?.Crc32 == entry.Entry.GetCrc32InHex()) {
                Logger.Debug(
                    $"File {entry.Entry.Key} already exists and has the same size ({entry.Entry.Size}) and crc32 ({entry.Entry.GetCrc32InHex()}). Skipped.");
                return false;
            }

            if (entry.File.Exists()) {
                Logger.Debug($"File {entry.Entry.Key} already exists locally. Skipped.");
                return false;
            }

            return true;
        }).ToList();

        var selected = SelectMany(entries,
            entry
                => $"{entry.Entry.Key}: {entry.Entry.Size} ({entry.Entry.GetCrc32InHex()}) => {entry.File}",
            new Func<List<(IArchiveEntry Entry, KifaFile File)>, string>(choices
                => $"entries ({choices.Sum(c => c.Entry.Size).ToSizeString()}) to extract"), selectionKey: "extract");

        var results = new KifaBatchActionResult();

        if (selected.Status != KifaActionStatus.OK) {
            results.Add("extract", selected);
            results.AddRange(RemoveArchiveFilesIfRequested(archive, archiveFile.ToString()));
            return results;
        }

        // The enumerator way is adopted due to the issue mentioned in
        // https://stackoverflow.com/a/44379540.
        using var reader = archive.ExtractAllEntries();
        var enumerator = selected.Value.GetEnumerator();
        var valid = enumerator.MoveNext();

        var extractedCount = 0;
        while (reader.MoveToNextEntry()) {
            if (valid && reader.Entry.Key == enumerator.Current.Entry.Key) {
                results.Add(reader.Entry.Key.Checked(), KifaActionResult.FromAction(() => {
                    var file = enumerator.Current.File;
                    var entry = enumerator.Current.Entry;

                    file.EnsureLocalParent();
                    var tempFile = file.GetIgnoredFile();

                    Logger.Debug($"Write {entry.Key} to {file}");
                    Logger.Debug($"Extract {entry.Key} to temp location {tempFile.GetLocalPath()}");
                    reader.WriteEntryTo(tempFile.GetLocalPath());
                    tempFile.Add();

                    var expectedCrc = entry.GetCrc32InHex();
                    if (tempFile.FileInfo?.Size != entry.Size ||
                        tempFile.FileInfo?.Crc32 != expectedCrc) {
                        throw new FileCorruptedException(
                            $"File {tempFile} should have size={entry.Size}, crc32={expectedCrc}, but has size={tempFile.FileInfo?.Size}, crc32={tempFile.FileInfo?.Crc32}.");
                    }

                    Logger.Debug(
                        $"File {tempFile} has the expected size={entry.Size} and crc32={expectedCrc}. Fast copy to {file}");
                    tempFile.Copy(file);

                    file.Add();
                    tempFile.Delete();
                    FileInformation.Client.RemoveLocation(tempFile.Id, tempFile.ToString());
                    Logger.LogResult(FileInformation.Client.Delete(tempFile.Id),
                        $"Removal of file info {tempFile.Id}");
                    extractedCount++;
                }));

                valid = enumerator.MoveNext();
            } else {
                Logger.Trace($"Ignored {reader.Entry.Key}");
            }
        }

        if (extractedCount < selected.Value.Count) {
            var missingFilesResult = new KifaActionResult {
                Status = KifaActionStatus.Error,
                Message =
                    $"Only extracted {extractedCount} files when {selected.Value.Count} is requested."
            };
            Logger.Error(missingFilesResult);
            results.Add(archiveFile.ToString(), missingFilesResult);
        } else if (results.IsAcceptable) {
            results.AddRange(RemoveArchiveFilesIfRequested(archive, archiveFile.ToString()));
        }

        return results;
    }

    IEnumerable<(string item, KifaActionResult result)> RemoveArchiveFilesIfRequested(
        IArchive archive, string archiveFile) {
        var volumeFiles = archive.Volumes.Select(v => v.FileName).ToList();

        // The check is needed due to https://github.com/adamhathcock/sharpcompress/issues/1331.
        if (volumeFiles.Any(f => f == null)) {
            Logger.Warn(
                $"Unexpected null volume files [{volumeFiles.JoinBy(", ")}]. Replaced with the original filename: {archiveFile}");
            volumeFiles = [archiveFile];
        }

        if (DeleteSource) {
            var toBeRemoved = SelectMany(volumeFiles, f => f,
                $"source archive files");

            if (toBeRemoved.Status != KifaActionStatus.OK) {
                return [("source archive files", toBeRemoved)];
            }

            return toBeRemoved.Value.Select(v => ($"Remove {v}", RemoveOneArchiveFile(new KifaFile(v))));
        }

        return [];
    }

    KifaActionResult RemoveOneArchiveFile(KifaFile file) {
        if (file.Registered) {
            if (Confirm($"File {file} is already registerd. Confirm removing it completely?")) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = "Should not remove a registered archive file as of now."
                };
            }

            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "Registered file is asked to be skipped."
            };
        }

        return KifaActionResult.FromAction(file.Delete);
    }
}

public static class IEntryExtension {
    public static string GetCrc32InHex(this IEntry entry) => ((int) entry.Crc).ToHexString();
}
