using System.Threading;
using Xunit;

namespace Kifa.Tests;

public class KifaShutdownTests {
    [Fact]
    public void RegisterAndRunCleanupsTest() {
        var called1 = 0;
        var called2 = 0;

        KifaShutdown.RegisterCleanup(() => Interlocked.Increment(ref called1));
        KifaShutdown.RegisterCleanup(() => Interlocked.Increment(ref called2));

        KifaShutdown.RunCleanups();

        Assert.Equal(1, called1);
        Assert.Equal(1, called2);

        // Subsequent calls should be no-op (run only once)
        KifaShutdown.RunCleanups();
        Assert.Equal(1, called1);
        Assert.Equal(1, called2);
    }
}
