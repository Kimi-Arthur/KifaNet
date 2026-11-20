namespace Kifa.Service;

// Everything a model's Fill() may want to know about how it is being called. Models use what they
// need and ignore the rest; callers without special needs use the no-argument DataModel.Fill().
public readonly record struct FillContext {
    // Fill as exhaustively as the model supports (`full=true`).
    public bool Full { get; init; }

    // Optional side-channel data, such as the raw source the model would otherwise fetch. It is a
    // plain string so it can travel anywhere; the model decides how to parse it. Models that have
    // no use for it ignore it (like Full), and act normally when it is null.
    public string? Input { get; init; }
}
