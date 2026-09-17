using Kifa.Tools;
using Kifa.Tools.MediaUtil.Commands;

KifaCommand.Run(args, typeof(ExtractAudioCommand), typeof(AddCoverCommand),
    typeof(CombineCommand), typeof(ViewCommand), typeof(FixInfoCommand),
    typeof(OrganizeCommand), typeof(CompareCommand));
