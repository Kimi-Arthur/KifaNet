namespace Kifa.ITerm;

public class ITermImage {
    // https://iterm2.com/3.2/documentation-images.html
    // https://stu.dev/displaying-images-in-iterm-from-dotnet-apps/
    public static string GetITermImageFromBase64(string encodedImage, int displayImageWidth = 0,
        int displayImageHeight = 0) {
        var option = "";
        if (displayImageWidth > 0) {
            option += $"width={displayImageWidth}px;";
        }

        if (displayImageHeight > 0) {
            option += $"height={displayImageHeight}px;";
        }

        return $"\u001B]1337;File=;{option};inline=1:{encodedImage.Split(",")[^1]}\u0007";
    }

    public static string GetITermImageFromRawBytes(byte[] data, int displayImageWidth = 0,
        int displayImageHeight = 0)
        => GetITermImageFromBase64(data.ToBase64(), displayImageWidth, displayImageHeight);
}
