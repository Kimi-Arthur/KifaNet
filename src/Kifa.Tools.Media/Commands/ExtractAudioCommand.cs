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

    public static int ImageSize { get; set; } = 256;

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

        var metadata = ExtractMetadata(sourceFile, trackNumber);
        var metadataString =
            string.Join(" ", metadata.Select(kv => $"-metadata {kv.Key}=\"{kv.Value}\""));

        var fileName = GetFileName(metadata);

        var targetFile = sourceFile.Parent.GetFile($"Albums/{fileName}.m4a");
        if (targetFile.Exists()) {
            return;
        }

        var coverFile = GetCover(sourceFile);
        var croppedImages = ImageCropper.Crop(coverFile);
        var chosenImage = ChooseImage(croppedImages);
        coverFile.Delete();

        var sourcePath = sourceFile.GetLocalPath();
        var targetPath = targetFile.GetLocalPath();
        Directory.GetParent(targetPath)!.Create();

        // Inline image: https://ffmpeg.org/ffmpeg-protocols.html#data
        var arguments = $"-i \"{sourcePath}\" -i \"{chosenImage}\" " +
                        $"-map 0:a -acodec copy -map 1 -c copy -disposition:v:0 attached_pic {metadataString} \"{targetPath}\"";
        Logger.Trace($"Executing: ffmpeg {arguments}");
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

    static string GetFileName(Dictionary<string, string> metadata)
        => $"{metadata["album"]}/{metadata["track"].PadLeft(2, '0')} {metadata["title"]}";

    // https://iterm2.com/3.2/documentation-images.html
    // https://stu.dev/displaying-images-in-iterm-from-dotnet-apps/
    static string ChooseImage(List<string> images)
        => SelectOne(images,
            image
                => $"\u001B]1337;File=;width={ImageSize}px;height={ImageSize}px;inline=1:{image[23..]}\u0007",
            "image").choice;

    static KifaFile GetCover(KifaFile file) {
        var aid = file.FileInfo.Metadata.Linking.Target.Split("-")[^1].Split(".")[0];
        var coverLink = new KifaFile(BilibiliVideo.Client.Get(aid).Cover.ToString());
        var coverFile = file.Parent.GetFile($"!{file.BaseName}.{coverLink.Extension}");
        if (!coverFile.Exists()) {
            coverLink.Copy(coverFile);
        }

        return coverFile;
    }

    static readonly Regex MusicFilePattern = new(@"\[[^\]]*\] (\d+-\d+-\d+)? (.*)");

    static Dictionary<string, string> ExtractMetadata(KifaFile file, int trackNumber) {
        var name = file.BaseName;
        var match = MusicFilePattern.Match(name);

        var artist = file.Path.Split("/")[^2];

        var date = match.Groups[1].Value;

        return new Dictionary<string, string> {
            { "title", match.Groups[2].Value },
            { "artist", artist },
            { "date", date },
            { "album", $"{artist} - {date[..4]}" },
            { "track", trackNumber.ToString() }
        };
    }
}
