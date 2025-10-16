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
        Assert.Equal("EEDBE3159BFF1B82ED7D862889E9E535", info.Md5);
        Assert.Equal("840B6700B2365FAAB27FB7E5F70D72539E91A6BD49950FFA26B98C49DDFAC536",
            info.Sha256);
    }
}
