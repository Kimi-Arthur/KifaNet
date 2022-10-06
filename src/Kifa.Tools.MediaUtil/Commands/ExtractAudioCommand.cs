using System.Diagnostics;
using System.Text.RegularExpressions;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Graphics;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.MediaUtil.Commands;

[Verb("audio", HelpText = "Extract audio from file.")]
public class ExtractAudioCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string DisplayImageSize { get; set; } = "20%";

    [Value(0, Required = true, HelpText = "Target file(s) to take action on.")]
    public IEnumerable<string> FileNames {
        get => Late.Get(fileNames);
        set => Late.Set(ref fileNames, value);
    }

    IEnumerable<string>? fileNames;

    public override int Execute() {
        var (multi, files) = KifaFile.FindExistingFiles(FileNames, recursive: false);
        files = files.Where(file => file.Extension != "m4a").ToList();
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
        var filesWithDates = files.Select(file => (file.BaseName.Split(" ")[1], file))
            .OrderBy(item => item.Item1).ToList();

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

        var fileName = GetFileName(metadata, sourceFile.BaseName.Split(" ")[0]);

        var targetFile = sourceFile.Parent.GetFile($"Albums/{fileName}.m4a");
        if (targetFile.Exists()) {
            return;
        }

        var coverFile = GetCover(sourceFile);
        var croppedImages = ImageCropper.Crop(coverFile);
        var chosenImage = ChooseImage(croppedImages);
        coverFile.Delete();
        coverFile.Write(chosenImage.Split(",")[^1].FromBase64());

        var sourcePath = sourceFile.GetLocalPath();
        var targetPath = targetFile.GetLocalPath();
        Directory.GetParent(targetPath)!.Create();

        // Inline image: https://ffmpeg.org/ffmpeg-protocols.html#data
        var arguments = $"-i \"{sourcePath}\" -i \"{coverFile.GetLocalPath()}\" " +
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

        coverFile.Delete();
    }

    static string GetFileName(Dictionary<string, string> metadata, string prefix)
        => $"{metadata["album"]}/{prefix} {metadata["track"].PadLeft(2, '0')} {metadata["title"]}";

    static string ChooseImage(List<string> images)
        => SelectOne(images,
            image => ITermImage.GetITermImageFromBase64(image, DisplayImageSize, DisplayImageSize),
            "image").choice;

    static KifaFile GetCover(KifaFile file) {
        var name = $"{KifaFile.DefaultIgnoredPrefix}{file.BaseName}";
        var coverPngFile = file.Parent.GetFile($"{name}.png");
        var coverJpgFile = file.Parent.GetFile($"{name}.jpg");
        if (coverPngFile.Exists()) {
            return coverPngFile;
        }

        if (coverJpgFile.Exists()) {
            return coverJpgFile;
        }

        var result = GetCoverFromEmbedded(file, name);
        if (result.Status == KifaActionStatus.OK) {
            return result.Response!;
        }

        result = GetCoverFromThumbnail(file, name);
        if (result.Status == KifaActionStatus.OK) {
            return result.Response!;
        }

        throw new Exception("Failed to extract raw cover image.");
    }

    static KifaActionResult<KifaFile> GetCoverFromEmbedded(KifaFile sourceFile, string name) {
        var extension = GetExtension(sourceFile);
        if (extension == null) {
            return new KifaActionResult<KifaFile> {
                Status = KifaActionStatus.Error,
                Message = "Failed to extract attached cover image. Maybe no cover is attached."
            };
        }

        var coverFile = sourceFile.Parent.GetFile($"{name}.{extension}");

        // https://superuser.com/a/1328212
        var arguments = $"-i \"{sourceFile.GetLocalPath()}\" " +
                        $"-map 0:v -map -0:V -c copy \"{coverFile.GetLocalPath()}\"";
        Logger.Trace($"Executing: ffmpeg {arguments}");
        using var proc = new Process {
            StartInfo = {
                FileName = "ffmpeg",
                Arguments = arguments
            }
        };

        proc.Start();
        proc.WaitForExit();
        return proc.ExitCode != 0
            ? new KifaActionResult<KifaFile> {
                Status = KifaActionStatus.Error,
                Message = "Failed to extract thumbnail as cover image."
            }
            : new KifaActionResult<KifaFile> {
                Status = KifaActionStatus.OK,
                Response = coverFile
            };
    }

    static readonly Regex CoverInfoRegex = new(@"Video: (\w+).*\(attached pic\)");

    static readonly Dictionary<string, string> ImageExtensionMapping = new() {
        { "mjpeg", "jpg" }
    };

    static string? GetExtension(KifaFile sourceFile) {
        var result = Executor.Run("ffprobe", $"\"{sourceFile.GetLocalPath()}\"");

        if (result.ExitCode != 0) {
            return null;
        }

        var match = CoverInfoRegex.Match(result.StandardError);
        if (!match.Success) {
            return null;
        }

        return ImageExtensionMapping.GetValueOrDefault(match.Groups[1].Value,
            match.Groups[1].Value);
    }

    static KifaActionResult<KifaFile> GetCoverFromThumbnail(KifaFile sourceFile, string name) {
        var coverFile = sourceFile.Parent.GetFile($"{name}.jpg");

        // https://ffmpeg.org/ffmpeg.html#:~:text=%2Dframes%5B%3Astream_specifier,after%20framecount%20frames.
        var arguments = $"-i \"{sourceFile.GetLocalPath()}\" " +
                        $"-frames:v 1 \"{coverFile.GetLocalPath()}\"";
        Logger.Trace($"Executing: ffmpeg {arguments}");
        using var proc = new Process {
            StartInfo = {
                FileName = "ffmpeg",
                Arguments = arguments
            }
        };

        proc.Start();
        proc.WaitForExit();
        return proc.ExitCode != 0
            ? new KifaActionResult<KifaFile> {
                Status = KifaActionStatus.Error,
                Message = "Failed to extract thumbnail as cover image."
            }
            : new KifaActionResult<KifaFile> {
                Status = KifaActionStatus.OK,
                Response = coverFile
            };
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
