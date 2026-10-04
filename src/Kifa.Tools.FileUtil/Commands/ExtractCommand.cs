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
public class ExtractCommand : KifaCommand {
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
        var selected = SelectMany(files,
            file => ShowSize ? $"{file} ({file.FileInfo?.Size.ToSizeString()})" : file.ToString(),
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
        var archive = ArchiveFactory.OpenArchive(archiveFile.GetLocalPath(), new ReaderOptions {
            Password = Password,
            ArchiveEncoding = new ArchiveEncoding {
                Default = Encoding
            }
        });

        var entries = archive.Entries.Where(entry => !entry.IsDirectory).Select(entry => (
            Entry: entry,
            File: folder.GetFile(ArchiveNameSeparator != null
                ? $"{archiveFile.BaseName}{ArchiveNameSeparator}{entry.Key.Checked()}"
                : entry.Key.Checked()))).Where(entry => {
                    Logger.Notice(()
                        => $"File:\t{entry.File} {entry.File.ExistsSomewhere()}, {entry.File.Exists(entry.Entry.Size)}");
                    Logger.Notice(()
                        => $"Expected:\tsize={entry.Entry.Size}, crc32={entry.Entry.GetCrc32InHex()}");
                    Logger.Notice(()
                        => $"Found:\tsize={entry.File.FileInfo?.Size}, crc32={entry.File.FileInfo?.Crc32}");

                    if (entry.File.ExistsSomewhere() && entry.File.FileInfo?.Size == entry.Entry.Size &&
                        (entry.Entry.GetCrc32InHex() == null ||
                         entry.File.FileInfo?.Crc32 == entry.Entry.GetCrc32InHex())) {
                        Logger.Debug(
                            $"File {entry.Entry.Key} already exists and has the same size ({entry.Entry.Size}) and crc32 ({entry.Entry.GetCrc32InHex()}). Skipped.");
                        return false;
                    }

                    if (entry.File.Exists(entry.Entry.Size)) {
                        Logger.Debug($"File {entry.Entry.Key} already exists locally. Skipped.");
                        return false;
                    }

                    return true;
                }).ToList();

        var selected = SelectMany(entries,
            entry
                => $"{entry.Entry.Key}: {entry.Entry.Size} ({entry.Entry.GetCrc32InHex()}) => {entry.File}",
            new Func<List<(IArchiveEntry Entry, KifaFile File)>, string>(choices
                => $"entries ({choices.Sum(c => c.Entry.Size).ToSizeString()}) to extract"));

        if (selected.Status != KifaActionStatus.OK) {
            var results = new KifaBatchActionResult();
            results.Add("extract", selected);
            results.AddRange(RemoveArchiveFilesIfRequested(archive, archiveFile.ToString()));
            return results;
        }

        try {
            return ExtractSolidArchive(archive, selected.Value, archiveFile.ToString());
        } catch (Exception ex) {
            Logger.Trace(ex,
                $"Failed to extract as solid archive: {ex.Message}. Falling back to normal archive extraction.");
            return ExtractNormalArchive(archive, selected.Value, archiveFile.ToString());
        }
    }

    KifaBatchActionResult ExtractSolidArchive(IArchive archive,
        List<(IArchiveEntry Entry, KifaFile File)> selected, string archiveFile) {
        var results = new KifaBatchActionResult();

        // The enumerator way is adopted due to the issue mentioned in
        // https://stackoverflow.com/a/44379540.
        using var reader = archive.ExtractAllEntries();
        var enumerator = selected.GetEnumerator();
        var valid = enumerator.MoveNext();

        var extractedCount = 0;
        while (reader.MoveToNextEntry()) {
            if (valid && reader.Entry.Key == enumerator.Current.Entry.Key) {
                var current = enumerator.Current;
                results.Add(reader.Entry.Key.Checked(), KifaActionResult.FromAction(() => {
                    ExtractOneEntry(current.Entry, current.File,
                        targetPath => reader.WriteEntryTo(targetPath));
                    extractedCount++;
                }));

                valid = enumerator.MoveNext();
            } else {
                Logger.Trace($"Ignored {reader.Entry.Key}");
            }
        }

        if (extractedCount < selected.Count || !results.IsAcceptable) {
            throw new Exception(
                $"Only extracted {extractedCount} files out of {selected.Count} requested, or encountered extraction errors.");
        }

        results.AddRange(RemoveArchiveFilesIfRequested(archive, archiveFile));
        return results;
    }

    KifaBatchActionResult ExtractNormalArchive(IArchive archive,
        List<(IArchiveEntry Entry, KifaFile File)> selected, string archiveFile) {
        var results = new KifaBatchActionResult();
        foreach (var (entry, file) in selected) {
            results.Add(entry.Key.Checked(),
                KifaActionResult.FromAction(() => {
                    ExtractOneEntry(entry, file, targetPath => entry.WriteToFile(targetPath));
                }));
        }

        if (results.IsAcceptable) {
            results.AddRange(RemoveArchiveFilesIfRequested(archive, archiveFile));
        }

        return results;
    }

    void ExtractOneEntry(IArchiveEntry entry, KifaFile file, Action<string> extractAction) {
        file.EnsureLocalParent();
        var tempFile = file.GetIgnoredFile();

        Logger.Debug($"Write {entry.Key} to {file}");
        Logger.Debug($"Extract {entry.Key} to temp location {tempFile.GetLocalPath()}");
        extractAction(tempFile.GetLocalPath());
        tempFile.Add(expectedSize: entry.Size);

        var expectedCrc = entry.GetCrc32InHex();
        if (tempFile.FileInfo?.Size != entry.Size ||
            (expectedCrc != null && tempFile.FileInfo?.Crc32 != expectedCrc)) {
            throw new FileCorruptedException(
                $"File {tempFile} should have size={entry.Size}, crc32={expectedCrc}, but has size={tempFile.FileInfo?.Size}, crc32={tempFile.FileInfo?.Crc32}.");
        }

        Logger.Debug(
            $"File {tempFile} has the expected size={entry.Size} and crc32={expectedCrc}. Fast copy to {file}");
        tempFile.Copy(file);

        file.Add(expectedSize: entry.Size);
        tempFile.Delete();
        FileInformation.Client.RemoveLocation(tempFile.Id, tempFile.ToString());
        Logger.LogResult(FileInformation.Client.Delete(tempFile.Id),
            $"Removal of file info {tempFile.Id}");
    }

    public IEnumerable<(string item, KifaActionResult result)> RemoveArchiveFilesIfRequested(
        IArchive archive, string archiveFile) {
        var volumeFiles = archive.Volumes.Select(v => v.FileName).ToList();

        // The check is needed due to https://github.com/adamhathcock/sharpcompress/issues/1331.
        if (volumeFiles.Any(f => f == null)) {
            Logger.Warn(
                $"Unexpected null volume files [{volumeFiles.JoinBy(", ")}]. Replaced with the original filename: {archiveFile}");
            volumeFiles = [archiveFile];
        }

        if (DeleteSource) {
            var files = new List<KifaFile>();
            var seen = new HashSet<string>();

            foreach (var v in volumeFiles) {
                var file = new KifaFile(v);
                if (file.Registered) {
                    foreach (var link in file.FileInfo.Checked().GetAllLinks()) {
                        if (seen.Add(link)) {
                            files.Add(link == file.Id
                                ? file
                                : new KifaFile(file.ToString(), id: link));
                        }
                    }
                } else {
                    if (seen.Add(file.ToString())) {
                        files.Add(file);
                    }
                }
            }

            var nonRegisteredIndices = files
                .Select((f, index) => (f, index))
                .Where(x => !x.f.Registered)
                .Select(x => (x.index + 1).ToString())
                .ToList();

            var defaultReply = nonRegisteredIndices.Count == files.Count
                ? "*"
                : string.Join(",", nonRegisteredIndices);

            var toBeRemoved = SelectMany(files,
                f => f.Registered ? f.Id : f.ToString(),
                choiceSummaryString: "source archive files to remove",
                defaultReply: defaultReply);

            if (toBeRemoved.Status != KifaActionStatus.OK) {
                return [("source archive files to remove", toBeRemoved)];
            }

            return toBeRemoved.Value.Checked().Select(f
                => ($"Remove {(f.Registered ? f.Id : f)}", RemoveOneArchiveFile(f)));
        }

        return [];
    }

    public KifaActionResult RemoveOneArchiveFile(KifaFile file) {
        if (file.Registered) {
            if (!Confirm(
                    $"File {file.Id} is already registered. Confirm removing it completely?",
                    suggested: false)) {
                return new KifaActionResult {
                    Status = KifaActionStatus.Skipped,
                    Message = $"Registered file {file.Id} is asked to be skipped."
                };
            }

            return file.RemoveLogical(force: true);
        }

        return KifaActionResult.FromAction(file.Delete);
    }
}

public static class IEntryExtension {
    public static string? GetCrc32InHex(this IEntry entry)
        => (int) entry.Crc == 0 || entry.Size == 0 ? null : ((int) entry.Crc).ToHexString();
}
