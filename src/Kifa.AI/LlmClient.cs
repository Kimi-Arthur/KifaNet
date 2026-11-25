using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Kifa.AI;

[JsonConverter(typeof(StringEnumConverter))]
public enum LlmConfidence {
    Low,
    Medium,
    High
}

public record LlmResponse(string Value, string Source);

public record LlmInfo {
    public required string Source { get; init; }
    public LlmConfidence? Confidence { get; init; }
    public string? Reason { get; init; }
}

public record LlmChoice(string Value, LlmInfo Info);

public interface LlmClient {
    LlmResponse? Ask(string prompt, string? jsonSchema = null, string? systemPrompt = null);

    LlmChoice? Choose(string prompt, List<string> choices) {
        if (choices.Count == 0) {
            return null;
        }

        var response = Ask(prompt, BuildChoiceSchema(choices),
            "Select the single best matching choice from the allowed list and respond with JSON {\"choice\": \"...\", \"confidence\": \"high\"|\"medium\"|\"low\", \"reason\": \"brief 1-sentence reason\"}.");
        if (response == null) {
            return null;
        }

        var parsed = ExtractValidChoice(response.Value, choices);
        if (parsed == null) {
            return null;
        }

        return new LlmChoice(parsed.Value.Choice, new LlmInfo {
            Source = response.Source,
            Confidence = parsed.Value.Confidence,
            Reason = parsed.Value.Reason
        });
    }

    internal static string BuildChoiceSchema(List<string> choices) {
        var schema = new JObject {
            ["type"] = "object",
            ["properties"] = new JObject {
                ["choice"] = new JObject {
                    ["type"] = "string",
                    ["enum"] = new JArray(choices)
                },
                ["confidence"] = new JObject {
                    ["type"] = "string",
                    ["enum"] = new JArray("high", "medium", "low")
                },
                ["reason"] = new JObject {
                    ["type"] = "string"
                }
            },
            ["required"] = new JArray("choice", "confidence", "reason"),
            ["additionalProperties"] = false
        };
        return schema.ToString(Formatting.None);
    }

    internal static (string Choice, LlmConfidence? Confidence, string? Reason)? ExtractValidChoice(
        string? raw, List<string> choices) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return null;
        }

        var trimmed = raw.Trim();
        if (choices.Contains(trimmed, StringComparer.Ordinal)) {
            return (trimmed, null, null);
        }

        try {
            var token = JToken.Parse(trimmed);
            if (token is JObject obj) {
                var direct = obj["choice"]?.Value<string>();
                if (!string.IsNullOrEmpty(direct) &&
                    choices.Contains(direct, StringComparer.Ordinal)) {
                    LlmConfidence? confidence =
                        Enum.TryParse<LlmConfidence>(obj["confidence"]?.Value<string>(),
                            ignoreCase: true, out var parsedConfidence)
                            ? parsedConfidence
                            : null;
                    var reason = obj["reason"]?.Value<string>()?.Trim();
                    return (direct, confidence, string.IsNullOrEmpty(reason) ? null : reason);
                }

                var nested = obj["result"]?.Value<string>();
                if (!string.IsNullOrEmpty(nested)) {
                    return ExtractValidChoice(nested, choices);
                }
            }
        } catch (JsonException) {
            // Fall through.
        }

        return null;
    }
}
