# Local Test Server Environment

Status: **Draft / proposal** (not implemented).

This document proposes an in-process, hermetic "local server" for tests, so that tests which exercise the data layer (`KifaServiceRestClient` → `Kifa.Web.Api` → `KifaServiceJsonClient` → disk) no longer depend on `www.kifa.ga`, the real `kimily.ch` server, or live third-party sites.

---

## 1. Problem Statement

Several test suites are written against a running server, or silently fall through to one:

| Suite | How it depends on a server today | Symptom |
| :--- | :--- | :--- |
| `Kifa.Service.Tests` (`DataModelTests`, `FakeDataModel`) | `FakeDataModelRestServiceClient` extends `KifaServiceRestClient‹T›` and uses the default `ServerAddress` (`http://www.kifa.ga/api`). | `DataModelGetBasicTest`, `DataModelCallBasicTest`, `DataModelPatchBasicTest` fail with a DNS error after ~20 s each (observed 2026-11-19). |
| `Kifa.YouTube.Tests` | Fake in-memory clients are installed per model, but the suite did not finish in 1 h 34 m when run locally (cause not yet diagnosed; likely a real network or process dependency such as yt-dlp or session leasing). | Hangs. |
| `Kifa.Tools.Tests` | Mixed: some use a mock `HttpMessageHandler` (`RemoteDataChefTests`, `CallCommandTests`), `KifaFileTests` touches `KifaServiceRestClient`. | Not verified in this session. |
| `Kifa.Bilibili.Tests` and similar | Call live Bilibili endpoints in some tests. | Flaky or failing offline. |
| `Kimily.Languages` (DWDS) | Snapshot parsing tests are offline, but nothing exercises the `DwdsPage` → `DwdsEtymologyPage` → `DwdsEtymology` → `DwdsGermanWord` chain against a real `KifaServiceJsonClient`. | Behaviour of pushed content (`Update` + `Fill`) is only covered indirectly. |

Consequences:
1. A normal `dotnet test` run is slow and noisy offline, so regressions are easy to miss.
2. Behaviour that only exists in the real client (freshness, `Update`/`Fill` interaction, link maintenance, `LinkedFields`) cannot be tested end to end across model layers.
3. We cannot reproduce server-side bugs with seeded data.

---

## 2. Goals and Non-goals

Goals:
1. **Hermetic**: tests run with no external network and no shared state.
2. **Real code path**: requests go through `KifaServiceRestClient`, the ASP.NET Core pipeline, `KifaDataController`, and `KifaServiceJsonClient` on a temp folder. No re-implemented fake of the server logic.
3. **Seedable per case**: each test (or collection) starts from a named, version-controlled data set.
4. **Controllable upstreams**: outbound scraping (DWDS, Bilibili, YouTube, ...) is served from fixtures, so `Fill` can run for real.
5. **Reusable across repos**: `KifaNet`, `Kimily.Languages` and `MitoNet` tests can use the same fixture.
6. **Fast**: in-process, no ports, parallel-safe.

Non-goals:
- Replacing pure unit tests that already use fakes where those are the clearest option.
- Emulating nginx, TLS client certificates, or the blue-green deployment.
- Load or concurrency testing beyond what unit tests cover.

---

## 3. Proposed Design

### 3.1 Host: `LocalKifaServer`

An in-process host built on `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory` over the `Program` class of `Kifa.Web.Api`, which is already `public`).

```csharp
public sealed class LocalKifaServer : IDisposable {
    public string DataFolder { get; }              // unique temp folder per instance
    public HttpClient Client { get; }              // wired to the in-memory TestServer
    public static LocalKifaServer Start(Action<LocalKifaServerOptions>? configure = null);
}
```

Responsibilities:
1. Create a unique `DataFolder` under the temp directory and point `KifaServiceJsonClient` at it (the server's data folder setting).
2. Register the model assemblies that the case needs (`Kifa.Languages.Dwds`, `Mito.Jav`, ...) so `KifaDataControllerFeatureProvider` discovers their models, the same way `Assemblies.LoadAll()` does in production.
3. Swap `KifaServiceRestClient.Client` to the `TestServer` handler and set `KifaServiceRestClient.ServerAddress` to the test base address; restore both on dispose.
4. Expose helpers: `Seed(...)`, `ReadRaw(modelId, id)`, `Reset()`.

### 3.2 Static state and isolation

`KifaServiceRestClient.Client`, `ServerAddress`, and each model's static `Client` property are process-wide, and `AppDomain`-wide reflection touches them (see the earlier flaky `FixVirtualLinks...` failure). To keep this safe:
1. Tests that use `LocalKifaServer` live in a named xUnit collection with `DisableParallelization = true`.
2. `LocalKifaServer` captures and restores every static it overwrites.
3. Model static clients are set explicitly to `KifaServiceRestClient‹T›` for the models under test, rather than relying on discovery side effects.

Longer term, consider moving the static `Client` lookup behind an ambient context (`AsyncLocal`) so collections could run in parallel. That is a separate refactor.

### 3.3 Seed data

Fixtures are plain files in the repository, in the same on-disk layout the server uses, so they double as documentation of the format:

```
Tests/Fixtures/{case}/
    {modelId}/{id}.json          # data + $metadata
    {modelId}/{id}.{suffix}      # external properties (for example .html)
    upstream/{host}/{path}       # canned upstream responses (see 3.4)
```

`Seed("dwds_bank")` copies a fixture tree into `DataFolder` before the first request. Fixtures can be created by `datax export` or by a small recording tool (see 3.5).

### 3.4 Fake upstreams

`Fill` implementations call third parties through `HttpClient` instances that are mostly `static readonly new HttpClient()` fields (for example `DwdsPage.HttpClient`). To make those controllable we need one seam:

1. Introduce a small shared factory, for example `KifaHttp.CreateClient(string name)`, that returns a client using a replaceable `HttpMessageHandler` (default: normal `SocketsHttpHandler`).
2. `LocalKifaServer` installs a `FixtureUpstreamHandler` that answers from `upstream/{host}/{path}` files and fails loudly (HTTP 599 plus a recorded error) for anything not in the fixture. This keeps tests honest: an unexpected outbound call is a test failure, not a silent network hit.
3. Each model migrates opportunistically: only models used by hermetic tests need to move off the bare `new HttpClient()`.

The handler also records every outbound request, so tests can assert "no scrape happened" (for example when pushed content is kept by `DwdsEtymologyPage.Fill`).

### 3.5 Fixture recording (optional tooling)

A `--record` mode on the handler that forwards to the real network once and stores the response under `upstream/`. Run manually when adding a new case; never in CI.

---

## 4. Scenarios the first iteration should cover

These are the cases that are currently untested end to end:

1. **Update fills on content change** (generic): patch a fresh item with changed content, so `Fill` runs and `Version` advances. A no-op patch does nothing.
2. **Upstream gone on update**: item is marked `Removed` in metadata, stored data is kept.
3. **Refresh options**: `refresh=true`, `full=true`, `IsRefreshRequested` visible to `Fill` only on explicit refresh.
4. **DWDS chain**:
   - First `Get` of a word: `DwdsPage` is scraped from fixture, embedded etymology entries are pushed into `DwdsEtymologyPage` (no second scrape), `DwdsEtymology` is parsed, `DwdsGermanWord` links to it.
   - Re-`Get` of the word with an unchanged page: no writes to `DwdsEtymologyPage`.
   - Changed embedded etymology HTML: `DwdsEtymologyPage.Version` advances and `DwdsEtymology` re-parses.
   - Non-headword (`Ankunft` → `kommen`) linking, and old-orthography headwords (`Schloss` → `Schloß`).
   - `ForceRefreshBefore` bump on `DwdsGermanWord` re-parses from cache with zero HTTP.
5. **Links**: `Set`/`Update` through an alias keep `Linking.Links`; `LinkedFields` expansion over real HTTP, including the 400 mapping for invalid paths.
6. **Existing server-dependent tests** (`Kifa.Service.Tests` `DataModelTests`) are rewritten on top of the local server instead of `www.kifa.ga`.

---

## 5. Phasing

| Phase | Deliverable | Exit criterion |
| :--- | :--- | :--- |
| 0 | Diagnose current failures: why `Kifa.YouTube.Tests` hangs; which `Kifa.Tools.Tests` and `Kifa.Bilibili.Tests` need the network. | A list of tests classified as unit, mock-based, or server/network-dependent. |
| 1 | `LocalKifaServer` (host, temp folder, static restore) plus a new `Kifa.Testing` helper project referenced by test projects. | `DataModelTests` pass offline against the local server. |
| 2 | Fixture seeding plus generic freshness scenarios (items 1 to 3 and 5 above). | Scenarios run in CI without network. |
| 3 | Upstream handler seam (`KifaHttp`) and `FixtureUpstreamHandler`; migrate DWDS models first. | DWDS chain scenarios (item 4) pass with no outbound calls. |
| 4 | Migrate `MitoNet` and other model families as needed; optional recording tool. | Offline `dotnet test` for the whole solution is green. |

---

## 6. Risks and Open Questions

1. **Static state**: the global `Client` properties make parallel hermetic servers impossible today. Accepted for now via non-parallel collections.
2. **Assembly discovery**: `KifaDataControllerFeatureProvider` scans `AppDomain` assemblies. The test host must load exactly the model assemblies the case needs; otherwise controllers for unrelated models appear. Needs a small option on the host.
3. **Configuration**: the server reads `KIFA_CONFIG` and expects cert paths and account configs. The test host needs a config-free startup path (or a minimal in-memory config).
4. **Auth**: production uses client certificates through nginx. The test host should bypass auth explicitly, in test-only code, rather than weakening the production pipeline.
5. **Fixture drift**: recorded upstream pages go stale. They should be treated like the existing `DwdsData` snapshots: regenerated deliberately, with a diff review.
6. **Scope of `Kifa.Testing`**: shared package versus source-linked files across repos. Prefer a project reference inside the existing `languages` worktree layout (as `Kimily.Languages` already references `KifaNet` projects).
7. **Unknown cause of the YouTube suite hang**: could be unrelated to the server (for example yt-dlp or lease timers). Phase 0 must establish this before promising that the local server fixes it.
</Content>
