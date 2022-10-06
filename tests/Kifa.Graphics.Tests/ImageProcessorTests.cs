using System.Drawing;
using Codeuctivity.ImageSharpCompare;
using SixLabors.ImageSharp;

namespace Kifa.Graphics.Tests;

public class ImageProcessorTests {
    [Fact]
    public void TestToBlackAndWhite() {
        using var sourceFile = File.OpenRead("a_source.bmp");
        using var referenceFile = File.OpenRead("a_reference.bmp");

        var reference = Image.Load(referenceFile);

        var result = Image.Load(sourceFile).ToBlackAndWhite();
        ImageSharpCompare.ImagesAreEqual(reference, result);
    }
}
