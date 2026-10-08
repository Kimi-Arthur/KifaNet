using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kifa.Service;
using Kifa.Tools.DataUtil;
using Xunit;

namespace Kifa.Tools.Tests;

[Collection("DataChefTests")]
public class RemoteDataChefTests : IDisposable {
    class MockHttpHandler : HttpMessageHandler {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public Func<HttpRequestMessage, HttpResponseMessage> ResponseHandler { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"status\":\"OK\"}")
            };

        protected override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            LastRequest = request;
            LastRequestBody = request.Content?.ReadAsStringAsync().Result;
            return ResponseHandler(request);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(Send(request, cancellationToken));
    }

    readonly MockHttpHandler mockHandler;
    readonly HttpClient originalClient;

    public RemoteDataChefTests() {
        mockHandler = new MockHttpHandler();
        originalClient = KifaServiceRestClient.Client;
        KifaServiceRestClient.Client = new HttpClient(mockHandler);
    }

    public void Dispose() {
        KifaServiceRestClient.Client = originalClient;
    }

    [Fact]
    public void GetChef_DynamicFallback_ReturnsRemoteDataChef() {
        var chef = DataChef.GetChef("unregistered/dynamic_model");
        chef.Should().NotBeNull();
        chef.Should().BeOfType<RemoteDataChef>();
        chef?.ModelId.Should().Be("unregistered/dynamic_model");

        var cachedChef = DataChef.GetChef("unregistered/dynamic_model");
        cachedChef.Should().BeSameAs(chef);
    }

    [Fact]
    public void Import_SendsPatchRequestWithCorrectPayload() {
        var chef = new RemoteDataChef("test_remote");
        var yaml = """
            # test_remote
            - Id: item1
              name: Item One
            - Id: item2
              name: Item Two
            """;

        var result = chef.Import(yaml);
        result.Status.Should().Be(KifaActionStatus.OK);

        mockHandler.LastRequest.Should().NotBeNull();
        mockHandler.LastRequest?.Method.Method.Should().Be("PATCH");
        mockHandler.LastRequest?.RequestUri?.ToString().Should()
            .Be("http://www.kifa.ga/api/test_remote/$");
        mockHandler.LastRequestBody.Should().Contain("item1");
        mockHandler.LastRequestBody.Should().Contain("Item One");
        mockHandler.LastRequestBody.Should().Contain("item2");
    }

    [Fact]
    public void Export_GetAll_PreservesOrderAndEnsuresId() {
        mockHandler.ResponseHandler = _ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("""
                {
                  "item_b": { "name": "Beta" },
                  "item_a": { "name": "Alpha" },
                  "item_c": { "name": "Gamma" }
                }
                """)
        };

        var chef = new RemoteDataChef("test_remote");
        var existingYaml = """
            # test_remote
            - Id: item_a
            - Id: item_b
            """;

        var result = chef.Export(existingYaml, getAll: true, compact: false);
        result.Status.Should().Be(KifaActionStatus.OK);

        mockHandler.LastRequest?.Method.Should().Be(HttpMethod.Get);
        mockHandler.LastRequest?.RequestUri?.ToString().Should()
            .Be("http://www.kifa.ga/api/test_remote/?recursive=True");

        var output = result.Value.Checked();
        output.Should().StartWith("# test_remote\n");

        // item_a should appear before item_b because existingYaml specified item_a first.
        var posA = output.IndexOf("Id: item_a", StringComparison.Ordinal);
        var posB = output.IndexOf("Id: item_b", StringComparison.Ordinal);
        var posC = output.IndexOf("Id: item_c", StringComparison.Ordinal);

        posA.Should().BeGreaterThan(-1);
        posB.Should().BeGreaterThan(posA);
        posC.Should().BeGreaterThan(posB);
    }

    [Fact]
    public void Export_SelectedIds_SendsGetWithIds() {
        mockHandler.ResponseHandler = _ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("""
                [
                  { "name": "First" },
                  { "name": "Second" }
                ]
                """)
        };

        var chef = new RemoteDataChef("test_remote");
        var inputYaml = """
            # test_remote
            - Id: id1
            - Id: id2
            """;

        var result = chef.Export(inputYaml, getAll: false, compact: false);
        result.Status.Should().Be(KifaActionStatus.OK);

        mockHandler.LastRequest?.Method.Should().Be(HttpMethod.Get);
        mockHandler.LastRequest?.RequestUri?.ToString().Should()
            .Be("http://www.kifa.ga/api/test_remote/$");
        mockHandler.LastRequestBody.Should().Contain("id1");
        mockHandler.LastRequestBody.Should().Contain("id2");

        var output = result.Value.Checked();
        output.Should().Contain("Id: id1");
        output.Should().Contain("Id: id2");
    }

    [Fact]
    public void Export_CompactFlowStyle_FormatsScalarLists() {
        mockHandler.ResponseHandler = _ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("""
                {
                  "item1": {
                    "tags": ["alpha", "beta"]
                  }
                }
                """)
        };

        var chef = new RemoteDataChef("test_remote");

        var compactResult = chef.Export("", getAll: true, compact: true);
        compactResult.Status.Should().Be(KifaActionStatus.OK);
        compactResult.Value.Checked().Should().Contain("tags: [alpha, beta]");

        var nonCompactResult = chef.Export("", getAll: true, compact: false);
        nonCompactResult.Status.Should().Be(KifaActionStatus.OK);
        nonCompactResult.Value.Checked().Should().Contain("- alpha\n");
    }

    [Fact]
    public void Link_SendsPostRequest() {
        var chef = new RemoteDataChef("test_remote");
        var result = chef.Link("source/target", "new/link");

        result.Status.Should().Be(KifaActionStatus.OK);
        mockHandler.LastRequest?.Method.Should().Be(HttpMethod.Post);
        mockHandler.LastRequest?.RequestUri?.ToString().Should()
            .Be("http://www.kifa.ga/api/test_remote/^");
        mockHandler.LastRequestBody.Should().Contain("source/target");
        mockHandler.LastRequestBody.Should().Contain("new/link");
    }

    [Fact]
    public void Delete_SendsDeleteRequest() {
        var chef = new RemoteDataChef("test_remote");
        var result = chef.Delete(["id1", "id2"]);

        result.Status.Should().Be(KifaActionStatus.OK);
        mockHandler.LastRequest?.Method.Should().Be(HttpMethod.Delete);
        mockHandler.LastRequest?.RequestUri?.ToString().Should()
            .Be("http://www.kifa.ga/api/test_remote/$");
        mockHandler.LastRequestBody.Should().Contain("id1");
        mockHandler.LastRequestBody.Should().Contain("id2");
    }

    [Fact]
    public void Call_WithoutParameters_SendsPost() {
        var chef = new RemoteDataChef("test_remote");
        var result = chef.Call("refresh");

        result.Status.Should().Be(KifaActionStatus.OK);
        mockHandler.LastRequest?.Method.Should().Be(HttpMethod.Post);
        mockHandler.LastRequest?.RequestUri?.ToString().Should()
            .Be("http://www.kifa.ga/api/test_remote/$refresh");
        mockHandler.LastRequest?.Content.Should().BeNull();
    }

    [Fact]
    public void Call_WithParameters_SendsPostWithJson() {
        var chef = new RemoteDataChef("test_remote");
        var result = chef.Call("refresh", "{\"force\": true}");

        result.Status.Should().Be(KifaActionStatus.OK);
        mockHandler.LastRequest?.Method.Should().Be(HttpMethod.Post);
        mockHandler.LastRequest?.RequestUri?.ToString().Should()
            .Be("http://www.kifa.ga/api/test_remote/$refresh");
        mockHandler.LastRequestBody.Should().Contain("\"force\":");
    }

    [Fact]
    public void Call_NotFound_ReturnsBadRequest() {
        mockHandler.ResponseHandler = _ => new HttpResponseMessage(HttpStatusCode.NotFound) {
            Content = new StringContent("{\"message\":\"Not found\"}")
        };

        var chef = new RemoteDataChef("test_remote");
        var result = chef.Call("unknown_action");

        result.Status.Should().Be(KifaActionStatus.BadRequest);
    }

    [Fact]
    public void Export_NotFound_ReturnsBadRequest() {
        mockHandler.ResponseHandler = _ => new HttpResponseMessage(HttpStatusCode.NotFound) {
            Content = new StringContent("{\"message\":\"Not found\"}")
        };

        var chef = new RemoteDataChef("test_remote");
        var result = chef.Export("", getAll: true, compact: false);

        result.Status.Should().Be(KifaActionStatus.BadRequest);
    }
}
