using CommandLine;
using Kifa.Tools;
using Kifa.Tools.Media.Commands;

KifaCommand.Run(Parser.Default.ParseArguments<ExtractAudioCommand, AddCoverCommand>, args);
