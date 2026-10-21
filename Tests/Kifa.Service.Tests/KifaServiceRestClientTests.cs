using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Kifa.Service.Tests;

public class KifaServiceRestClientTests {
    [Fact]
    public async Task Client_ConnectsWithTcpKeepAlive() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () => {
            using var serverSocket = await listener.AcceptSocketAsync();
            var buffer = new byte[1024];
            _ = await serverSocket.ReceiveAsync(buffer);
            var response =
                "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK"u8.ToArray();
            await serverSocket.SendAsync(response);
        });

        var body = await KifaServiceRestClient.Client.GetStringAsync($"http://127.0.0.1:{port}/");
        body.Should().Be("OK");
        await serverTask;
    }
}
