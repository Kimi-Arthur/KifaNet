using System.Diagnostics;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.Media.Commands;

[Verb("audio", HelpText = "Extract audio from file.")]
public class ExtractAudioCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, HelpText = "Target file(s) to take action on.")]
    public IEnumerable<string> FileNames {
        get => Late.Get(fileNames);
        set => Late.Set(ref fileNames, value);
    }

    IEnumerable<string>? fileNames;

    public override int Execute() {
        var (multi, files) = KifaFile.ExpandFiles(FileNames, recursive: false);
        if (multi) {
            foreach (var file in files) {
                Console.WriteLine(file);
            }

            Console.Write($"Confirm extracting audio from the {files.Count} files above?");
            Console.ReadLine();
        }

        var failedFiles = new List<KifaFile>();
        foreach (var file in files) {
            try {
                ExtractAudioFile(file);
            } catch (Exception ex) {
                Logger.Error(ex, $"Failed to extract audio from {file}");
                failedFiles.Add(file);
            }
        }

        if (failedFiles.Count > 0) {
            Logger.Error($"Failed to extract audio from {failedFiles.Count} files:");
            foreach (var file in failedFiles) {
                Logger.Error($"\t{file}");
            }

            return 1;
        }

        Logger.Info($"Successfully extracted audio from {files.Count} files.");
        return 0;
    }

    static void ExtractAudioFile(KifaFile sourceFile) {
        var targetFile = sourceFile.Parent.GetFile($"{sourceFile.BaseName}.m4a");

        var sourcePath = ((FileStorageClient) sourceFile.Client).GetPath(sourceFile.Path);
        var targetPath = ((FileStorageClient) targetFile.Client).GetPath(targetFile.Path);

        var arguments = $"-i \"{sourcePath}\" -map 0:a -acodec copy \"{targetPath}\"";
        Logger.Debug($"Executing: ffmpeg {arguments}");
        using var proc = new Process {
            StartInfo = {
                FileName = "ffmpeg",
                Arguments = arguments
            }
        };

        proc.Start();
        proc.WaitForExit();
        if (proc.ExitCode != 0) {
            throw new Exception("Extract audio file failed.");
        }
    }
}
