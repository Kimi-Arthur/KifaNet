using System.Diagnostics;
using Kifa.Api.Files;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Kifa.Tools.Media;

public class ImageCropper {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #region public late static string CropCommand { get; set; }

    static string? cropCommand;

    public static string CropCommand {
        get => Late.Get(cropCommand);
        set => Late.Set(ref cropCommand, value);
    }

    #endregion

    #region public late static string CropArguments { get; set; }

    static string? cropArguments;

    public static string CropArguments {
        get => Late.Get(cropArguments);
        set => Late.Set(ref cropArguments, value);
    }

    #endregion

    #region public late static string RemoteServer { get; set; }

    static string? remoteServer;

    public static string RemoteServer {
        get => Late.Get(remoteServer);
        set => Late.Set(ref remoteServer, value);
    }

    #endregion

    public static List<KifaFile> Crop(KifaFile sourceFile) {
        using var image = Image.Load(sourceFile.GetLocalPath(), out var format);

        var middleFile =
            sourceFile.Parent.GetFile($"{sourceFile.BaseName}.middle.{sourceFile.Extension}");

        if (!middleFile.Exists()) {
            using var croppedMiddle = CropMiddle(image);
            croppedMiddle.Save(middleFile.GetLocalPath(), format);
        }

        var leftFile =
            sourceFile.Parent.GetFile($"{sourceFile.BaseName}.left.{sourceFile.Extension}");

        if (!leftFile.Exists()) {
            using var croppedLeft = CropLeft(image);
            croppedLeft.Save(leftFile.GetLocalPath(), format);
        }

        var rightFile =
            sourceFile.Parent.GetFile($"{sourceFile.BaseName}.right.{sourceFile.Extension}");
        if (!rightFile.Exists()) {
            using var croppedRight = CropRight(image);
            croppedRight.Save(rightFile.GetLocalPath(), format);
        }

        var deepFile =
            sourceFile.Parent.GetFile($"{sourceFile.BaseName}.deep.{sourceFile.Extension}");
        if (!deepFile.Exists()) {
            using var croppedDeep = CropDeep(sourceFile, image);
            croppedDeep.Save(deepFile.GetLocalPath(), format);
        }

        return new List<KifaFile> {
            middleFile,
            leftFile,
            rightFile,
            deepFile
        };
    }

    public static Image CropMiddle(Image image) {
        var (width, height) = image.Size();

        return image.Clone(p => p.Crop(new Rectangle((width - height) / 2, 0, height, height)));
    }

    public static Image CropLeft(Image image) {
        var (_, height) = image.Size();

        return image.Clone(p => p.Crop(new Rectangle(0, 0, height, height)));
    }

    public static Image CropRight(Image image) {
        var (width, height) = image.Size();

        return image.Clone(p => p.Crop(new Rectangle(width - height, 0, height, height)));
    }

    public static Image CropDeep(KifaFile sourceFile, Image image) {
        var remoteFile = new KifaFile($"{RemoteServer}{sourceFile.Path}");
        sourceFile.Copy(remoteFile);

        var arguments = CropArguments.Format(new Dictionary<string, string> {
            { "image_path", remoteFile.GetRemotePath() }
        });
        Logger.Debug($"Executing: {CropCommand} {arguments}");
        using var proc = new Process {
            StartInfo = {
                FileName = CropCommand,
                Arguments = arguments
            }
        };
        proc.StartInfo.RedirectStandardOutput = true;
        proc.Start();
        proc.WaitForExit();
        if (proc.ExitCode != 0) {
            throw new Exception("Failed to crop image.");
        }

        var lines = proc.StandardOutput.ReadToEnd().Split("\n");

        return CropToRectangle(image, ExtractValue(lines, "x0: "), ExtractValue(lines, "y0: "),
            ExtractValue(lines, "x1: "), ExtractValue(lines, "y1: "));
    }

    static Image CropToRectangle(Image image, double x0, double y0, double x1, double y1) {
        var originalSize = image.Size();

        return image.Clone(p => p.Crop(new Rectangle((originalSize.Width * x0).Round(),
            (originalSize.Height * y0).Round(), (originalSize.Width * (x1 - x0)).Round(),
            (originalSize.Height * (y1 - y0)).Round())));
    }

    static double ExtractValue(string[] lines, string key)
        => double.Parse(lines.First(line => line.Contains(key)).Split(" ").Last());
}

public static class ImageExtensions {
    public static void Save(this Image image, string filePath, IImageFormat format) {
        using var stream = File.OpenWrite(filePath);
        image.Save(stream, format);
    }
}