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

    Encoding? encoding;
    Encoding Encoding => encoding ??= GetArchiveEncoding();

    [Option('s', "archive-separator",
        HelpText =
            "Separator between archive name and entry name. Default is not prepend archive name.")]
    public string? ArchiveNameSeparator { get; set; }

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames);
        var selected = SelectMany(files,
            choiceToString: file => $"{file} ({file.FileInfo?.Size.ToSizeString()})",
            choicesName: "files to extract from");

        foreach (var file in selected) {
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

        var volumes = archive.Volumes.ToList();

        Logger.Error($"Archive volume count: {volumes.Count}");
        foreach (var volume in volumes) {
            Logger.Error($"Volume name: {volume.Index} {volume.FileName}");
        }

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
            choiceToString: entry
                => $"{entry.Entry.Key}: {entry.Entry.Size} ({entry.Entry.GetCrc32InHex()}) => {entry.File}",
            choicesName: "entries to extract");

        if (selected.Count == 0) {
            return new KifaActionResult {
                Status = KifaActionStatus.Skipped,
                Message = "No more files selected to be extracted."
            };
        }

        var results = new KifaBatchActionResult();

        // The enumerator way is adopted due to the issue mentioned in
        // https://stackoverflow.com/a/44379540.
        using var reader = archive.ExtractAllEntries();
        var enumerator = selected.GetEnumerator();
        var valid = enumerator.MoveNext();

        while (reader.MoveToNextEntry()) {
            if (valid && reader.Entry.Key == enumerator.Current.Entry.Key) {
                results.Add(reader.Entry.Key.Checked(), KifaActionResult.FromAction(() => {
                    var file = enumerator.Current.File;
                    var entry = enumerator.Current.Entry;

                    file.EnsureLocalParent();
                    var tempFile = file.GetIgnoredFile();

                    Logger.Debug($"Write {entry.Key} to {file}");
                    Logger.Trace($"Extract {entry.Key} to temp location {tempFile.GetLocalPath()}");
                    reader.WriteEntryTo(tempFile.GetLocalPath());
                    tempFile.Add();

                    var expectedCrc = entry.GetCrc32InHex();
                    if (tempFile.FileInfo?.Size != entry.Size ||
                        tempFile.FileInfo?.Crc32 != expectedCrc) {
                        throw new FileCorruptedException(
                            $"File {tempFile} should have size={entry.Size}, crc32={expectedCrc}, but has size={tempFile.FileInfo?.Size}, crc32={tempFile.FileInfo?.Crc32}.");
                    }

                    Logger.Debug(
                        $"File {tempFile} passed size and crc32 check. Fast copy to {file}");
                    tempFile.Copy(file);

                    file.Add();
                    tempFile.Delete();
                    FileInformation.Client.RemoveLocation(tempFile.Id, tempFile.ToString());
                    Logger.LogResult(FileInformation.Client.Delete(tempFile.Id),
                        $"Removal of file info {tempFile.Id}");
                }));

                valid = enumerator.MoveNext();
            } else {
                Logger.Trace($"Ignored {reader.Entry.Key}");
            }
        }

        // results.AddRange(RemoveArchives(archive.Volumes
        //     .Select(v => folder.GetFile(v.FileName.Checked())).ToList()));

        return results;
    }

    IEnumerable<(string item, KifaActionResult result)> RemoveArchives(
        List<KifaFile> archiveVolumes) {
        var selected = SelectMany(archiveVolumes, v => $"{v}: {v.Length}",
            "archive files to delete since extraction is successful");
        if (selected.Count == 0) {
            Logger.Info("No archive files to remove.");
        }

        return selected.Select(v => ($"Remove {v}", RemoveOneArchiveFile(v)));
    }

    KifaActionResult RemoveOneArchiveFile(KifaFile file) {
        if (file.Registered) {
            if (Confirm($"File {file} is already registerd. Confirm removing it completely?")) {
                return new KifaActionResult {
                    Status = KifaActionStatus.BadRequest,
                    Message = "Should not extract from registered archives as of now."
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
