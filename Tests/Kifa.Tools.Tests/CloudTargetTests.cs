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
    public void ParseTeleV2_ReturnsCorrectTarget() {
        var target = CloudTarget.Parse("tele.v2");
        Assert.Equal(CloudServiceType.Tele, target.ServiceType);
        Assert.Equal(KifaFileV2Format.Instance, target.FormatType);
        Assert.Equal("tele.v2", target.ToString());
    }
}
