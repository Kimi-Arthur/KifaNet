using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Kifa.AI.Tests;

public class LlmClientTests {
    sealed class StubHttpHandler(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
        : HttpMessageHandler {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) {
            var body = request.Content != null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : "";
            return handler(request, body);
        }
    }

    [Fact]
    public void OpenAiLlmClientChooseSendsStructuredSchemaAndValidatesResult() {
        string? capturedUrl = null;
        string? capturedAuth = null;
        string? capturedBody = null;
        using var httpClient = new HttpClient(new StubHttpHandler((req, body) => {
            capturedUrl = req.RequestUri?.ToString();
            capturedAuth = req.Headers.Authorization?.ToString();
            capturedBody = body;
            var responseJson = new JObject {
                ["choices"] = new JArray {
                    new JObject {
                        ["message"] = new JObject {
                            ["content"] =
                                "{\"choice\":\"übersetzen#1\",\"confidence\":\"high\",\"reason\":\"Matches ferrying across water.\"}"
                        }
                    }
                }
            }.ToString(Formatting.None);
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }));

        LlmClient client = new OpenAiLlmClient(httpClient) {
            Endpoint = "http://localhost:11434/v1/",
            Model = "qwen3:4b",
            ApiKey = "test-key"
        };

        var result = client.Choose("Pick the matching etymology word.",
            ["übersetzen#1", "übersetzen#2"]);

        result.Should().NotBeNull();
        result!.Value.Should().Be("übersetzen#1");
        result.Info.Source.Should().Be("llm:qwen3:4b");
        result.Info.Confidence.Should().Be(LlmConfidence.High);
        result.Info.Reason.Should().Be("Matches ferrying across water.");
        capturedUrl.Should().Be("http://localhost:11434/v1/chat/completions");
        capturedAuth.Should().Be("Bearer test-key");
        capturedBody.Should().Contain("\"enum\":[\"übersetzen#1\",\"übersetzen#2\"]");
        capturedBody.Should().Contain("\"enum\":[\"high\",\"medium\",\"low\"]");
    }

    [Fact]
    public void OpenAiLlmClientChooseRejectsInvalidChoice() {
        using var httpClient = new HttpClient(new StubHttpHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"content\":\"{\\\"choice\\\":\\\"invalid#9\\\"}\"}}]}",
                    Encoding.UTF8, "application/json")
            }));

        LlmClient client = new OpenAiLlmClient(httpClient) {
            Endpoint = "http://localhost:11434/v1",
            Model = "qwen3:4b"
        };

        client.Choose("Pick one.", ["übersetzen#1", "übersetzen#2"]).Should().BeNull();
    }

    [Fact]
    public void AgyLlmClientChooseInvokesCliWithJsonSchemaAndUnwrapsEnvelope() {
        List<string>? capturedArgs = null;
        LlmClient client = new AgyLlmClient(psi => {
            capturedArgs = [..psi.ArgumentList];
            return (0,
                "{\"status\":\"SUCCESS\",\"response\":\"{\\\"choice\\\":\\\"übersetzen#1\\\",\\\"confidence\\\":\\\"medium\\\",\\\"reason\\\":\\\"Ferrying sense.\\\"}\",\"structured_output\":{\"choice\":\"übersetzen#1\",\"confidence\":\"medium\",\"reason\":\"Ferrying sense.\"}}",
                "");
        }) {
            Model = "gemini-3.8-flash-low"
        };

        var result = client.Choose("Pick the matching etymology word.",
            ["übersetzen#1", "übersetzen#2"]);

        result.Should().NotBeNull();
        result!.Value.Should().Be("übersetzen#1");
        result.Info.Source.Should().Be("agy:gemini-3.8-flash-low");
        result.Info.Confidence.Should().Be(LlmConfidence.Medium);
        result.Info.Reason.Should().Be("Ferrying sense.");
        capturedArgs.Should().NotBeNull();
        capturedArgs.Should().Contain("-p");
        capturedArgs.Should().Contain("--json-schema");
        capturedArgs.Should().Contain("--model");
        capturedArgs.Should().Contain("gemini-3.8-flash-low");
        // Prompt must immediately follow -p.
        var pIndex = capturedArgs!.IndexOf("-p");
        pIndex.Should().Be(capturedArgs.Count - 2);
    }

    [Fact]
    public void AgyLlmClientAskSupportsGeneralFreeTextQuestions() {
        LlmClient client = new AgyLlmClient(_ =>
            (0, "{\"status\":\"SUCCESS\",\"response\":\"42\\n\"}", "")) {
            Model = "gemini-3.8-flash-low"
        };

        var result = client.Ask("What is 6 * 7?");

        result.Should().NotBeNull();
        result!.Value.Should().Be("42");
        result.Source.Should().Be("agy:gemini-3.8-flash-low");
    }
}
