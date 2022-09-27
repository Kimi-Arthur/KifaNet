namespace Kifa.ITerm;

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
}
