namespace Kifa.ITerm;

public class ITermImage {
    public static string GetITermImageFromBase64(string encodedImage, int displayImageWidth,
        int displayImageHeight)
        => $"\u001B]1337;File=;width={displayImageWidth}px;height={displayImageHeight}px;inline=1:{encodedImage.Split(",")[^1]}\u0007";
}
