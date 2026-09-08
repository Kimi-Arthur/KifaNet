# Data Freshness Refresh Failure Handling

## 1. Background
KifaNet uses a two-timestamp freshness model (`Metadata.Version` and `Metadata.LastRefreshed`) to manage upstream data synchronization without unnecessary file writes or network calls.

When an upstream entity (e.g., a deleted or private YouTube video, an archived Bilibili video, or a removed dictionary word) becomes unavailable after being previously stored, calling `Get(id)` or background scans would trigger `data.Fill()`.

### The Problem
Previously:
1. `data.Fill()` threw a `DataNotFoundException`.
2. `KifaServiceJsonClient.Fill()` caught this exception and returned `false`.
3. Because `false` was returned, `Metadata.LastRefreshed` was never updated on disk.
4. Consequently, on every subsequent `Get(id)` call, `data.NeedRefresh()` evaluated to `true`, repeatedly triggering heavy network operations (`yt-dlp`, archive lookups, Wayback Machine) only to fail again.

---

## 2. Design & Solution

### Data Lifecycle Status (`DataStatus`)
We introduce an explicit status enum in `DataMetadata`:
```csharp
public enum DataStatus {
    OK = 0,
    NotFound = 1,
    Removed = 2
}
```
- `OK` (0): Default status for normal, active data. Omitted from serialization as the default enum value (`DefaultValue = 0`).
- `NotFound` (1): Negative cache / tombstone for entities that do not exist upstream.
- `Removed` (2): Previously existing items with historical data whose remote resource was deleted/made private.

---

### Handling Failures in `KifaServiceJsonClient.Fill()`

When `data.Fill()` encounters `DataNotFoundException`:

1. **Non-Existing / Empty Items (`isEmpty` or `Status == DataStatus.NotFound`)**:
   - An item is considered empty if it has no `Metadata.Version` and `data.IsEmpty()` evaluates to `true` (no content properties populated).
   - `Metadata.Status = DataStatus.NotFound`
   - `Metadata.Version = now`
   - `Metadata.LastRefreshed = now`
   - Persists the empty tombstone `{ "id": "...", "$metadata": { "status": "not_found", "version": "..." } }` to disk.
   - `Get(id)` returns `null` (caller receives 404/null).
   - **Negative Caching**: Subsequent `Get(id)` requests read the cached tombstone from disk. Because `NeedRefresh()` is `false` during `RefreshInterval`, `Fill()` is skipped completely (zero upstream scraping or network traffic).

2. **Existing Items (including legacy unversioned items with content)**:
   - For items with existing content (or previously versioned items):
   - `Metadata.Status = DataStatus.Removed`
   - `Metadata.Version ??= now` (assigns a new version timestamp for legacy unversioned items, preserving previous version for already versioned items).
   - `Metadata.LastRefreshed = now`
   - Persists `{ "id": "...", ..., "$metadata": { "status": "removed", "version": "...", "last_refreshed": "..." } }` to disk.
   - `Get(id)` returns the historical cached content.

3. **Recovery / Upstream Re-emergence (`refresh: true` or after `RefreshInterval`)**:
   - When `Fill()` succeeds after an entity becomes available:
     - `Metadata.Status = DataStatus.OK` (omitted from serialized output).
     - `Metadata.Version = now` and `Metadata.LastRefreshed = now`.
     - `Get(id)` returns the populated entity.

---

### Handling in `List()` and `Get()`
- **`Get(id)`**:
  If the retrieved model has `Metadata.Status == DataStatus.NotFound`, `Get()` returns `null`.
- **`List()`**:
  Tombstoned items (`Metadata.Status == DataStatus.NotFound`) are filtered out (`.Where(i => i.Metadata?.Status != DataStatus.NotFound)`), while active (`OK`) and `Removed` items (containing historical metadata) are returned.

---

### Freshness Evaluation (`NeedRefresh`)
In `DataFreshnessExtensions.NeedRefresh()`:
```csharp
var lastChecked = data.Metadata.LastRefreshed ?? data.Metadata.Version;
if (data.ForceRefreshBefore != null && lastChecked < data.ForceRefreshBefore) {
    return true;
}

if (data.RefreshInterval != null) {
    return (lastChecked + data.RefreshInterval)?.Value < DateTimeOffset.UtcNow;
}
```
- `lastChecked` uses `LastRefreshed` (falling back to `Version`).
- Once `LastRefreshed` is updated after a failed fill attempt, subsequent calls will not trigger `Fill()` until `RefreshInterval` (e.g. 365 days for videos) has elapsed.
- Even when `ForceRefreshBefore` is set (e.g. following code logic updates), `lastChecked < ForceRefreshBefore` ensures the attempt runs only once after the code change, rather than repeating on every request.

### Exception Granularity
- **`DataNotFoundException`**:
  Predefined business/domain condition indicating the resource definitively does not exist upstream. Status is advanced (`NotFound` or `Removed`) and persisted to disk.
- **`FailedToFillException`**:
  Process execution failure (such as incomplete page downloads, parsing structure mismatches, or missing auth tokens). Status and timestamps are **not** advanced, preserving historical data without tombstoning.
- **General `Exception`**:
  Unexpected runtime errors (such as unhandled socket errors or runtime bugs). Status and timestamps are **not** advanced.

---

## 3. Related Files & Tests
- Implementation:
  - [`src/Kifa.Service/DataModel/DataStatus.cs`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataStatus.cs)
  - [`src/Kifa.Service/DataModel/DataMetadata.cs`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataMetadata.cs)
  - [`src/Kifa.Web.Api/KifaServiceJsonClient.cs`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Web.Api/KifaServiceJsonClient.cs)
- Tests:
  - [`tests/Kifa.Web.Api.Tests/KifaServiceJsonClientTests/VersioningAndFreshnessTests.cs`](file:///Users/jingbian/Projects/KifaNet/tests/Kifa.Web.Api.Tests/KifaServiceJsonClientTests/VersioningAndFreshnessTests.cs)
