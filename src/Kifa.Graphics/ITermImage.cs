using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kifa.Graphics;

public class ITermImage {
    // https://iterm2.com/3.2/documentation-images.html
    // https://stu.dev/displaying-images-in-iterm-from-dotnet-apps/
    public static string GetITermImageFromBase64(string encodedImage,
        string? displayImageWidth = null, string? displayImageHeight = null) {
        var option = "";
        if (displayImageWidth != null) {
            option += $"width={displayImageWidth};";
        }

        if (displayImageHeight != null) {
            option += $"height={displayImageHeight};";
        }

        return $"\u001B]1337;File=;{option};inline=1:{encodedImage.Split(",")[^1]}\u0007";
    }

    public static string GetITermImageFromRawBytes(byte[] data, string? displayImageWidth = null,
        string? displayImageHeight = null)
        => GetITermImageFromBase64(data.ToBase64(), displayImageWidth, displayImageHeight);

    public static string GetITermImagesSideBySideFromRawBytes(byte[] leftBytes, byte[] rightBytes,
        string? displayImageWidth = null, string? displayImageHeight = null) {
        var leftImage = Image.Load(leftBytes);
        var rightImage = Image.Load(rightBytes);
        var outputImage = new Image<Rgba32>(width: leftImage.Width + rightImage.Width,
            height: Math.Max(leftImage.Height, rightImage.Height));
        outputImage.Mutate(output => {
            output.DrawImage(leftImage, new Point(0, 0), 1);
            output.DrawImage(rightImage, new Point(leftImage.Width, 0), 1);
        });

        using var ms = new MemoryStream();
        outputImage.Save(ms, PngFormat.Instance);

        return GetITermImageFromBase64(ms.ToByteArray().ToBase64(), displayImageWidth,
            displayImageHeight);
    }
}
