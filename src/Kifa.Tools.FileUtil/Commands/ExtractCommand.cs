using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using NLog;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("extract", HelpText = "Extract files and add to system.")]
class ExtractCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to upload.")]
    public IEnumerable<string> FileNames { get; set; }

    [Option('s', "skip-full", HelpText = "Skip full file check after extraction.")]
    public bool SkipFull { get; set; } = false;

    [Option('p', "password", HelpText = "Password for the archive.")]
    public string? Password { get; set; }

    [Option('e', "encoding",
        HelpText = "Encoding of the archive, typically for zip files with GB18030.")]
    public string? ArchiveEncoding { get; set; }

    Encoding? encoding;
    Encoding Encoding => encoding ??= GetArchiveEncoding();

    [Option('p', "prefix-archive",
        HelpText =
            "Whether to prefix the output files/folders with the archive name. The value is the separator.")]
    public string? PrefixArchiveName { get; set; }

    public override int Execute(KifaTask? task = null) {
        var files = KifaFile.FindExistingFiles(FileNames);
        var selected = SelectMany(files,
            choiceToString: file => $"{file} ({file.FileInfo?.Size.ToSizeString()})",
            choiceName: "files");

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

    void ExtractFile(KifaFile archiveFile) {
        var folder = archiveFile.Parent;
        using var archiveFileStream = archiveFile.OpenRead();
        var archive = ArchiveFactory.Open(archiveFileStream, new ReaderOptions {
            Password = Password,
            ArchiveEncoding = new ArchiveEncoding(Encoding, Encoding)
        });
        var entries = archive.Entries.ToList();

        var selected = SelectMany(entries,
            choiceToString: entry
                => $"{entry.Key}: {entry.Size} ({((int) entry.Crc).ToHexString()})",
            choiceName: "entries");
        foreach (var entry in selected) {
            var fileName = PrefixArchiveName != null
                ? $"{archiveFile.BaseName}{PrefixArchiveName}{entry.Key.Checked()}"
                : entry.Key.Checked();
            var file = folder.GetFile(fileName);
            var tempFile = file.GetIgnoredFile();
            Logger.Debug($"Write {entry.Key} to {tempFile.GetLocalPath()}");
            entry.WriteToFile(tempFile.GetLocalPath());
            Logger.Debug($"Copy {tempFile.GetLocalPath()} to {file}");
            tempFile.Copy(file);
            file.Add(knownInfo: new FileInformation {
                Size = entry.Size,
                Crc32 = ((int) entry.Crc).ToHexString()
            });
            tempFile.Delete();
        }
    }
}
