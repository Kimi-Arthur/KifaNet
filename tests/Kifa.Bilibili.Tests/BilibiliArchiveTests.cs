using FluentAssertions;
using Kifa.Bilibili.BilibiliApi;
using Kifa.Configs;
using Xunit;

namespace Kifa.Bilibili.Tests;

public class BilibiliArchiveTests {
    public BilibiliArchiveTests() {
        KifaConfigs.Init();
    }

    [Fact]
    public void RpcTest() {
        HttpClients.BilibiliHttpClient
            .Call(new ArchiveRpc(uploaderId: "43536", seasonId: "1808473")).Data.Aids.Should()
            .HaveCountGreaterOrEqualTo(6);
    }
}
