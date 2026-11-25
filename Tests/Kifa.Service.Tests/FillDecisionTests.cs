using System;
using System.Collections.Generic;
using FluentAssertions;
using Kifa.AI;
using Xunit;

namespace Kifa.Service.Tests;

public class FillDecisionTests {
    sealed class TestModel : DataModel {
        public override TimeSpan? RefreshInterval => RefreshIntervals.None;
    }

    sealed class TestQuestion(string promptText, List<string> choices) : FillDecisionQuestion {
        public override string Type => "SelectEtymologyWord";
        public override string Key => "SelectEtymologyWord:übersetzen#1:setzen";
        public override List<string> Choices => choices;
        public override Dictionary<string, object> BuildInputs() => new() { ["candidates"] = choices };
        public override string BuildPrompt() => promptText;
    }

    sealed class StubLlmClient(Func<string, LlmResponse?> ask) : LlmClient {
        public int CallCount { get; private set; }

        public LlmResponse? Ask(string prompt, string? jsonSchema = null,
            string? systemPrompt = null) {
            CallCount++;
            return ask(prompt);
        }
    }

    [Fact]
    public void FindFillDecisionMatchesAfterJsonRoundTripAndInvalidatesOnInputChange() {
        const string key = "SelectEtymologyWord:übersetzen#1:setzen";
        var model = new TestModel { Id = "übersetzen" };
        var inputs = new Dictionary<string, object> {
            ["candidates"] = new List<string> { "übersetzen#1", "übersetzen#2" }
        };
        model.SetFillDecision(key, new FillDecision {
            Inputs = inputs,
            Value = "übersetzen#1",
            Llm = new LlmInfo {
                Source = "llm:qwen3:4b",
                Confidence = LlmConfidence.High,
                Reason = "Matches ferrying sense."
            }
        });

        var json = model.ToPrettyJson();
        var roundTripped = json.FromJson<TestModel>();
        roundTripped.Should().NotBeNull();

        var hit = roundTripped!.FindFillDecision(key, inputs);
        hit.Should().NotBeNull();
        hit!.Value.Should().Be("übersetzen#1");
        hit.Llm.Should().NotBeNull();
        hit.Llm!.Source.Should().Be("llm:qwen3:4b");
        hit.Llm.Confidence.Should().Be(LlmConfidence.High);
        hit.Llm.Reason.Should().Be("Matches ferrying sense.");

        // Changing candidates invalidates the cached decision.
        var changedInputs = new Dictionary<string, object> {
            ["candidates"] = new List<string> { "übersetzen#1", "übersetzen#2", "übersetzen#3" }
        };
        roundTripped.FindFillDecision(key, changedInputs).Should().BeNull();
    }

    [Fact]
    public void ResolveDecisionCachesResultAndReusesOnSubsequentCalls() {
        var model = new TestModel { Id = "übersetzen" };
        var llm = new StubLlmClient(_ => new LlmResponse(
            "{\"choice\":\"übersetzen#1\",\"confidence\":\"high\",\"reason\":\"Direct match.\"}",
            "llm:test"));
        var question = new TestQuestion("Pick one.", ["übersetzen#1", "übersetzen#2"]);

        model.ResolveDecision(question, llm).Should().Be("übersetzen#1");
        llm.CallCount.Should().Be(1);

        var cached = model.Metadata!.Decisions![question.Key];
        cached.Llm.Should().NotBeNull();
        cached.Llm!.Source.Should().Be("llm:test");
        cached.Llm.Confidence.Should().Be(LlmConfidence.High);
        cached.Llm.Reason.Should().Be("Direct match.");

        // Second call hits cache with 0 LLM invocations.
        model.ResolveDecision(question, llm).Should().Be("übersetzen#1");
        llm.CallCount.Should().Be(1);
    }
}
