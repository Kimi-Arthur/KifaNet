using Kifa.Api.Files;
using Kifa.IO.FileFormats;
using Xunit;

namespace Kifa.Tools.Tests;

public class CloudTargetTests {

    [Fact]
    public void ParseGoogleV1_ReturnsCorrectTarget() {
        var target = CloudTarget.Parse("google.v1");
        Assert.Equal(CloudServiceType.Google, target.ServiceType);
        Assert.Equal(KifaFileV1Format.Instance, target.FormatType);
        Assert.Equal("google.v1", target.ToString());
    }

    [Fact]
    public void ParseSwissV2_ReturnsCorrectTarget() {
        var target = CloudTarget.Parse("swiss.v2");
        Assert.Equal(CloudServiceType.Swiss, target.ServiceType);
        Assert.Equal(KifaFileV2Format.Instance, target.FormatType);
        Assert.Equal("swiss.v2", target.ToString());
    }
}
