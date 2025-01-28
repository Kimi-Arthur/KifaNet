using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Jobs;
using NLog;
using SharpCompress.Archives;
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

    void ExtractFile(KifaFile archiveFile) {
        var folder = archiveFile.Parent;
        using var archiveFileStream = archiveFile.OpenRead();
        var archive = ArchiveFactory.Open(archiveFileStream, new ReaderOptions {
            Password = Password
        });
        var entries = archive.Entries.ToList();

        var selected = SelectMany(entries,
            choiceToString: entry
                => $"{entry.Key}: {entry.Size} ({((int) entry.Crc).ToHexString()})",
            choiceName: "entries");
        foreach (var entry in selected) {
            var file = folder.GetFile(entry.Key.Checked());
            var tempFile = file.GetIgnoredFile();
            entry.WriteToFile(tempFile.GetLocalPath());
            tempFile.Copy(file);
            file.Add(knownInfo: new FileInformation {
                Size = entry.Size,
                Crc32 = ((int) entry.Crc).ToHexString()
            });
        }
    }
}
