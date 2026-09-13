using Kifa.IO.StorageClients;
using Xunit;

namespace Kifa.IO.Tests.StorageClients;

public class WebStorageClientTest {
    [Fact]
    public void ReadTest() {
        var info = FileInformation.GetInformation(new WebStorageClient {
            Protocol = "https"
        }.OpenRead("//cdn.duden.de/_media_/audio/ID4111794_361730273.mp3"), FileProperties.All);
        Assert.Equal(25703, info.Size);
        Assert.Equal("00D9F96646EB63B515B69F193D6503BC", info.Md5);
        Assert.Equal("73905B7CBABF9B68046527D110D195BDBEFD1878F6D7258B3967D9E8882C9D87",
            info.Sha256);
    }
}
