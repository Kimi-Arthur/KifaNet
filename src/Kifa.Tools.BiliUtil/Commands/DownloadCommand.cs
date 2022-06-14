using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;

namespace Kifa.Tools.BiliUtil.Commands;

public abstract class DownloadCommand : KifaCommand {
    [Option('d', "prefix-date", HelpText = "Prefix file name with the upload date.")]
    public bool PrefixDate { get; set; } = false;

    [Option('a', "all-staff", HelpText = "Put video in all folders owned by any staff.")]
    public bool AllStaff { get; set; } = false;

    [Option('s', "source", HelpText = "Override default source choice.")]
    public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;

    [Option('o', "output-folder",
        HelpText = "Folder to output video files to. Defaults to current folder.")]
    public string? OutputFolder { get; set; }

    public DownloadOptions DownloadOptions
        => new() {
            PrefixDate = PrefixDate,
            SourceChoice = SourceChoice,
            OutputFolder = OutputFolder != null ? new KifaFile(OutputFolder) : CurrentFolder
        };
}
