using CommandLine;
using Kifa.Tools;
using Kifa.Tools.MediaUtil.Commands;

KifaCommand.Run(
    Parser.Default
        .ParseArguments<ExtractAudioCommand, AddCoverCommand, CombineCommand, ViewCommand,
            FixInfoCommand>, args);
