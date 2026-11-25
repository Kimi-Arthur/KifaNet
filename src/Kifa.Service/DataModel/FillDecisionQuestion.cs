using System.Collections.Generic;

namespace Kifa.Service;

// Base class for precompiled, strongly-typed fill decision questions (mirroring RPC definitions).
// Subclasses define the question key, readable cache inputs, allowed choices, and prompt builder;
// DataModel.ResolveDecision() handles cache lookup, validation, LlmClient invocation, and caching.
public abstract class FillDecisionQuestion {
    public abstract string Type { get; }
    public abstract string Key { get; }
    public abstract List<string> Choices { get; }

    public abstract Dictionary<string, object> BuildInputs();
    public abstract string BuildPrompt();
}
