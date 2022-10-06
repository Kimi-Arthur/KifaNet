using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Kifa.Graphics;

public static class ImageProcessor {
    public static float BlackAndWhiteThreshold { get; set; } = 0.99f;

    public static Image ToBlackAndWhite(this Image source) {
        source.Mutate(i => i.BinaryThreshold(BlackAndWhiteThreshold));

        return source;
    }
}
