using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Kifa.AI;

// Calls the native Gemini API (/v1beta/models/{model}:generateContent) with optional
// standard JSON Schema structured output (`responseJsonSchema`).
public class GeminiLlmClient : LlmClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string DefaultEndpoint { get; set; } =
        "https://generativelanguage.googleapis.com/v1beta";

    public static string DefaultModel { get; set; } = "gemini-3.5-flash-lite";
    public static string? DefaultApiKey { get; set; }

    public string Endpoint { get; set; } = DefaultEndpoint;
    public string Model { get; set; } = DefaultModel;
    public string? ApiKey { get; set; } = DefaultApiKey;
    public int TimeoutSeconds { get; set; } = 60;

    readonly HttpClient? customClient;

    public GeminiLlmClient(HttpClient? httpClient = null) {
        customClient = httpClient;
    }

    public LlmResponse? Ask(string prompt, string? jsonSchema = null, string? systemPrompt = null) {
        var apiKey = ApiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrWhiteSpace(Endpoint) ||
            string.IsNullOrWhiteSpace(Model) || string.IsNullOrWhiteSpace(apiKey)) {
            return null;
        }

        var url = $"{Endpoint.TrimEnd('/')}/models/{Model}:generateContent";
        var generationConfig = new JObject {
            ["temperature"] = 0
        };
        if (!string.IsNullOrWhiteSpace(jsonSchema)) {
            generationConfig["responseMimeType"] = "application/json";
            generationConfig["responseJsonSchema"] = JObject.Parse(jsonSchema);
        }

        var payload = new JObject {
            ["contents"] = new JArray {
                new JObject {
                    ["role"] = "user",
                    ["parts"] = new JArray {
                        new JObject { ["text"] = prompt }
                    }
                }
            },
            ["generationConfig"] = generationConfig
        };
        if (!string.IsNullOrWhiteSpace(systemPrompt)) {
            payload["system_instruction"] = new JObject {
                ["parts"] = new JArray {
                    new JObject { ["text"] = systemPrompt }
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
        request.Headers.Add("x-goog-api-key", apiKey);

        try {
            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) {
                Logger.Warn($"GeminiLlmClient failed ({(int) response.StatusCode}): {body}");
                return null;
            }

            var root = JObject.Parse(body);
            var parts = root["candidates"]?[0]?["content"]?["parts"] as JArray;
            if (parts == null) {
                return null;
            }

            var content = string.Concat(parts
                .OfType<JObject>()
                .Where(p => p["thought"]?.Value<bool>() != true)
                .Select(p => p["text"]?.Value<string>())
                .Where(t => !string.IsNullOrEmpty(t))).Trim();
            if (string.IsNullOrWhiteSpace(content)) {
                return null;
            }

            var modelVersion = root["modelVersion"]?.Value<string>();
            var modelTag = !string.IsNullOrWhiteSpace(modelVersion) ? modelVersion : Model;
            return new LlmResponse(content, $"gemini:{modelTag}");
        } catch (Exception ex) {
            Logger.Warn(ex, "GeminiLlmClient error.");
            return null;
        }
    }
}
