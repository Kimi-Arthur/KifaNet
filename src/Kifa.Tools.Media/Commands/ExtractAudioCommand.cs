using System.Diagnostics;
using System.Text.RegularExpressions;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Bilibili;
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
        files = files.Where(file => file.Extension == "mp4").ToList();
        if (multi) {
            foreach (var file in files) {
                Console.WriteLine(file);
            }

            Console.Write($"Confirm extracting audio from the {files.Count} files above?");
            Console.ReadLine();
        }

        var failedFiles = new List<KifaFile>();

        var trackNumbers = GatherTrackNumbers(files);

        foreach (var file in files) {
            try {
                ExtractAudioFile(file, trackNumbers[file.ToString()]);
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

    Dictionary<string, int> GatherTrackNumbers(List<KifaFile> files) {
        var filesWithDates = files
            .Select(file => (
                new KifaFile(file.ToString()).FileInfo.Metadata.Linking.Target.Split("/")[^1]
                    .Split(" ")[0], file)).OrderBy(item => item.Item1).ToList();

        var lastYear = "";
        var lastTrack = 0;

        var results = new Dictionary<string, int>();
        foreach (var (date, file) in filesWithDates) {
            var year = date[..4];
            if (year != lastYear) {
                lastYear = year;
                lastTrack = 1;
            }

            results[file.ToString()] = lastTrack++;
        }

        return results;
    }

    static void ExtractAudioFile(KifaFile sourceFile, int trackNumber) {
        sourceFile = new KifaFile(sourceFile.ToString());
        var targetFile = sourceFile.Parent.GetFile($"{sourceFile.BaseName}.m4a");
        if (targetFile.Exists()) {
            return;
        }

        var coverFile = GetCover(sourceFile);
        var croppedFiles = ImageCropper.Crop(coverFile);
        var finalCoverFile =
            sourceFile.Parent.GetFile($"{coverFile.BaseName}.final.{coverFile.Extension}");

        if (!finalCoverFile.Exists()) {
            croppedFiles[0].Copy(finalCoverFile);
        }

        var metadata = string.Join(" ",
            ExtractMetadata(sourceFile, trackNumber)
                .Select(kv => $"-metadata {kv.Key}=\"{kv.Value}\""));

        var sourcePath = sourceFile.GetLocalPath();
        var targetPath = targetFile.GetLocalPath();
        var squareCoverPath = finalCoverFile.GetLocalPath();

        var arguments =
            $"-i \"{sourcePath}\" -i \"{squareCoverPath}\" -map 0:a -acodec copy -map 1 -c copy -disposition:v:0 attached_pic {metadata} \"{targetPath}\"";
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

    static KifaFile GetCover(KifaFile file) {
        var aid = file.FileInfo.Metadata.Linking.Target.Split("-")[^1].Split(".")[0];
        var coverLink = new KifaFile(BilibiliVideo.Client.Get(aid).Cover.ToString());
        var coverFile = file.Parent.GetFile($"{file.BaseName}.{coverLink.Extension}");
        if (!coverFile.Exists()) {
            coverLink.Copy(coverFile);
        }

        return coverFile;
    }

    static readonly Regex MusicFilePattern = new(@"\[[^\]]*\] (.*)");

    static Dictionary<string, string> ExtractMetadata(KifaFile file, int trackNumber) {
        var name = file.BaseName;
        var match = MusicFilePattern.Match(name);

        var artist = file.Path.Split("/")[^2];

        var date = file.FileInfo.Metadata.Linking.Target.Split("/")[^1].Split(" ")[0];
        var year = date[..4];

        return new Dictionary<string, string> {
            { "title", match.Groups[1].Value },
            { "artist", artist },
            { "date", date },
            { "album", $"{artist} - {date[..4]}" },
            { "track", trackNumber.ToString() }
        };
    }
}
