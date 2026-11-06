# Scattered Refresh Scheduling

This document proposes an extension to the [Versioning and Freshness System](2026-08-29%20Versioning%20and%20Freshness%20System.md) in KifaNet to prevent synchronized refresh spikes ("thundering herds") across large datasets such as dictionary pages and lexicon entries.

---

## 1. Problem Statement

Currently, `DataModel` defines a fixed `RefreshInterval` per model type:

```csharp
public class CambridgePage : DataModel, WithModelId<CambridgePage> {
    public override TimeSpan? RefreshInterval => TimeSpan.FromDays(365);
}
```

And `GetNextRefresh()` calculates expiration using that constant interval:

```csharp
public virtual DataVersion? GetNextRefresh() =>
    Metadata?.Version != null && RefreshInterval != null
        ? (Metadata.LastRefreshed ?? Metadata.Version) + RefreshInterval
        : null;
```

When a batch job (e.g., importing 5,000 German words or scraping 10,000 dictionary pages) fills a large collection of items on the same day:
1. Every item receives `Version` / `LastRefreshed` within the same few hours.
2. Exactly `RefreshInterval` (e.g., 365 days) later, **all 5,000 items become stale simultaneously**.
3. Subsequent accesses or maintenance jobs trigger a massive spike of upstream network scraping and LLM processing instead of spreading refreshes smoothly over time.

---

## 2. Design Goals

1. **Zero Metadata Pollution**: Keep `$metadata` clean—do not store precomputed `next_refresh` timestamps in JSON files on disk (preserving Principle 2.2 of the Versioning and Freshness System).
2. **Deterministic Per-Item Scattering**: Given the same `Id` and `RefreshInterval`, `GetNextRefresh()` must always return the same timestamp across processes and machines.
3. **Tiered / Cascading Compatibility**: Work seamlessly with `NeedRefreshFrom(upstream)` so downstream models (`GermanWord`) only refresh when upstream source models (`DwdsGermanWord`, `CambridgeGlobalGermanWord`, etc.) actually change content (`Version` advances).

---

## 3. Proposed Design: Deterministic Hash Jitter

### 3.1 `RefreshJitter` Property on `DataModel`

Introduce a virtual `RefreshJitter` property (defaulting to a fraction of `RefreshInterval`, e.g., $\pm 20\%$ or $+0\dots 50\%$, or configurable per model):

```csharp
[JsonIgnore]
[YamlIgnore]
public virtual TimeSpan? RefreshInterval => null;

// Optional window over which to scatter refreshes deterministically based on Id.
// E.g. RefreshInterval = 365d, RefreshJitter = 180d -> actual interval in [365d, 545d).
[JsonIgnore]
[YamlIgnore]
public virtual TimeSpan? RefreshJitter => RefreshInterval != null
    ? TimeSpan.FromTicks(RefreshInterval.Value.Ticks / 2)
    : null;
```

### 3.2 Deterministic Offset Calculation

Because `string.GetHashCode()` in modern .NET is randomized per process execution, we use a stable deterministic hash (e.g., FNV-1a or SHA256/MD5 prefix of ` $"{ModelId}/{RealId}" `):

```csharp
public virtual TimeSpan? GetEffectiveRefreshInterval() {
    if (RefreshInterval == null) {
        return null;
    }

    if (RefreshJitter == null || RefreshJitter.Value.Ticks <= 0 || string.IsNullOrEmpty(RealId)) {
        return RefreshInterval;
    }

    var stableHash = GetStableHash(RealId); // UInt64 FNV-1a hash
    var offsetTicks = (long)(stableHash % (ulong)RefreshJitter.Value.Ticks);
    return RefreshInterval.Value + TimeSpan.FromTicks(offsetTicks);
}

public virtual DataVersion? GetNextRefresh() =>
    Metadata?.Version != null && GetEffectiveRefreshInterval() is { } interval
        ? (Metadata.LastRefreshed ?? Metadata.Version) + interval
        : null;
```

---

## 4. Interaction with Multi-Tier Data Pipelines

In a multi-tier pipeline (`Raw Page` $\rightarrow$ `Parsed Source Word` $\rightarrow$ `Core Word`):

1. **Layer 0 — Raw Pages (`DwdsPage`, `CambridgePage`, `WiktionaryPage`)**:
   - Set `RefreshInterval = TimeSpan.FromDays(365)` (effective interval scattered across `365..545` days).
   - Background maintenance or lazy reads only re-fetch a small fraction ($\sim 1/180$) of pages on any given day after the first year.
   - If the re-fetched HTML has not meaningfully changed, `LastRefreshed` advances to `UtcNow` and the item enters its next `365..545` day cycle.
2. **Layer 1 — Parsed Source Models (`DwdsGermanWord`, `DeWiktionaryGermanWord`)**:
   - Only re-parse when `NeedRefreshFrom(rawPage)` is true (i.e., `rawPage.Metadata.Version > this.Metadata.Version`) or when parser code changes (`ForceRefreshBefore` is bumped at release time).
   - If re-parsing produces identical structured data (`Equals(originalContent)`), `Metadata.Version` does **not** advance—stopping unnecessary downstream cascades!
3. **Layer 2 — Core Models (`GermanWord`)**:
   - Only re-run `Fill()` (and its LLM sense-clustering step) when `NeedRefreshFrom(sourceWord)` is true for at least one upstream source, or when `GermanWord.ForceRefreshBefore` is bumped.
