using CommandLine;
using Kifa.Bilibili;

namespace Kifa.Tools.BiliUtil.Commands;

public abstract class DownloadCommand : KifaCommand {
    [Option('d', "prefix-date", HelpText = "Prefix file name with the upload date.")]
    public bool PrefixDate { get; set; } = false;

    [Option('a', "all-staff", HelpText = "Put video in all folders owned by any staff.")]
    public bool AllStaff { get; set; } = false;

    [Option('s', "source", HelpText = "Override default source choice.")]
    public int SourceChoice { get; set; } = BilibiliVideo.DefaultBiliplusSourceChoice;
}
