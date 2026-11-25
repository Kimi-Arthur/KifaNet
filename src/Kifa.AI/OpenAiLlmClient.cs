using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Kifa.AI;

// Calls any OpenAI-compatible /chat/completions endpoint (Google AI Studio Gemini free tier,
// local Ollama / llama-server, OpenAI, OpenRouter, etc.).
public class OpenAiLlmClient : LlmClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string DefaultEndpoint { get; set; } = "http://localhost:11434/v1";
    public static string DefaultModel { get; set; } = "qwen3:4b";
    public static string? DefaultApiKey { get; set; }

    public string Endpoint { get; set; } = DefaultEndpoint;
    public string Model { get; set; } = DefaultModel;
    public string? ApiKey { get; set; } = DefaultApiKey;
    public int TimeoutSeconds { get; set; } = 60;

    readonly HttpClient? customClient;

    public OpenAiLlmClient(HttpClient? httpClient = null) {
        customClient = httpClient;
    }

    public LlmResponse? Ask(string prompt, string? jsonSchema = null, string? systemPrompt = null) {
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(Endpoint) ||
            string.IsNullOrWhiteSpace(Model)) {
            return null;
        }

        var url = Endpoint.TrimEnd('/') + "/chat/completions";
        var messages = new JArray();
        if (!string.IsNullOrWhiteSpace(systemPrompt)) {
            messages.Add(new JObject {
                ["role"] = "system",
                ["content"] = systemPrompt
            });
        }

        messages.Add(new JObject {
            ["role"] = "user",
            ["content"] = prompt
        });

        var payload = new JObject {
            ["model"] = Model,
            ["temperature"] = 0,
            ["messages"] = messages
        };
        if (!string.IsNullOrWhiteSpace(jsonSchema)) {
            payload["response_format"] = new JObject {
                ["type"] = "json_schema",
                ["json_schema"] = new JObject {
                    ["name"] = "llm_response",
                    ["strict"] = true,
                    ["schema"] = JObject.Parse(jsonSchema)
                }
            };
        }

        HttpClient? ownedClient = null;
        var client = customClient ?? (ownedClient = new HttpClient {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        });
        using var _ = ownedClient;

        using var request = new HttpRequestMessage(HttpMethod.Post, url) {
            Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8,
                "application/json")
        };
        if (!string.IsNullOrWhiteSpace(ApiKey)) {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        }

        try {
            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) {
                Logger.Warn($"OpenAiLlmClient failed ({(int) response.StatusCode}): {body}");
                return null;
            }

            var root = JObject.Parse(body);
            var content = root["choices"]?[0]?["message"]?["content"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(content)) {
                return null;
            }

            var resolvedModel = root["model"]?.Value<string>();
            var modelTag = !string.IsNullOrWhiteSpace(resolvedModel) ? resolvedModel : Model;
            return new LlmResponse(content.Trim(), $"llm:{modelTag}");
        } catch (Exception ex) {
            Logger.Warn(ex, "OpenAiLlmClient error.");
            return null;
        }
    }
}
