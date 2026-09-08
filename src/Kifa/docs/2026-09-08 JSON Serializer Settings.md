# JSON Serializer Settings in KifaNet

## Overview
JSON serialization and deserialization across KifaNet are centralized in [`KifaJsonSerializerSettings`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/KifaJsonSerializerSettings.cs) and powered by [`OrderedContractResolver`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/OrderedContractResolver.cs).

This document explains the shared base configuration, all preconfigured settings profiles, how each profile handles different types of data, and their specific use cases across the codebase.

---

## Shared Base Configuration

Every settings profile is built via `KifaJsonSerializerSettings.GetSettings(...)` and shares a consistent baseline:

| Option | Configuration | Purpose |
|---|---|---|
| **Date & Time** | `DateFormatString = "yyyy-MM-dd HH:mm:ss.ffffff"`<br>`DateTimeZoneHandling = Utc` | Enforces ISO-like UTC timestamps with microsecond precision. |
| **Converters** | `StringEnumConverter(SnakeCase)`<br>`GenericJsonConverter` | Enums serialize as `snake_case` strings (e.g., `not_found`, `ok`). Polymorphic types use generic converter resolution. |
| **Null Values** | `NullValueHandling = NullValueHandling.Ignore` | Properties with `null` values are omitted from output JSON. |
| **Missing Members** | `MissingMemberHandling = MissingMemberHandling.Ignore` | Allows forward/backward compatibility when deserializing payloads with unrecognized fields. |
| **Object Creation** | `ObjectCreationHandling = ObjectCreationHandling.Replace` | Replaces target collections during deserialization instead of appending to existing collections. |
| **Equality Comparer** | `ReferenceEqualityComparer.Instance` | Prevents circular reference loops by tracking object identities. |

---

## Contract Resolver Capabilities ([`OrderedContractResolver`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/OrderedContractResolver.cs))

All serializer settings use `OrderedContractResolver`, which adds the following behaviors to property serialization:

1. **Deterministic Ordering**: Properties are ordered predictably for deterministic JSON diffs and on-disk consistency.
2. **Empty Collection Suppression**:
   - For **non-nullable collection properties** (e.g., `List<T>`, `HashSet<T>`, `SortedSet<T>`, `Dictionary<K, V>`, `T[]`), empty instances (`Count == 0`) are automatically excluded from serialization as they represent the default unpopulated state.
   - For **nullable collection properties** (e.g., `List<T>?`, `SortedSet<T>?`, `Dictionary<K, V>?`), `null` values are omitted, while explicitly instantiated empty collections (`new()`) are preserved to allow intentionally serializing empty `[]` or `{}`.
3. **Default Value Suppression**:
   - Non-nullable value types and enums (e.g., `DataStatus.OK = 0`) have default values populated so that default values are omitted when `DefaultValueHandling.IgnoreAndPopulate` or `Ignore` is active.
4. **External Property Suppression**:
   - Controlled by `IgnoreExternalProperties` (defaults to `false`). When explicitly enabled (as in `Disk`), properties decorated with `[ExternalProperty]` are suppressed via `property.ShouldSerialize = _ => false` so they are not written to the main JSON file on disk.
5. **Metadata Filtering**:
   - Configurable via `IgnoredProperties`. Can strip `$metadata` / `Metadata` entirely (as in `DataContent`).

---

## Settings Profiles & Data Handling Matrix

| Profile | Naming | Formatting | Default Value Handling | `[ExternalProperty]` | Metadata | Primary Use Case |
|---|---|---|---|---|---|---|
| **`Default`** | `snake_case` | Compact | `IgnoreAndPopulate` | **Included** | Included | General serialization (`ToJson()`), deserialization (`FromJson()`), RPC payloads, and deep cloning (`Clone()`). |
| **`Pretty`** | `snake_case` | Indented (2 spaces) | `IgnoreAndPopulate` | **Included** | Included | ASP.NET Web API controller responses (`Startup.cs`), CLI debug inspection, `ToPrettyJson()`. |
| **`Disk`** | `snake_case` | Indented (2 spaces) | `IgnoreAndPopulate` | **Ignored** | Included | Writing `.json` data files to local disk storage in `KifaServiceJsonClient`. |
| **`DataContent`** | `snake_case` | Compact | `IgnoreAndPopulate` | **Included** | **Stripped** (`$metadata`) | Content hashing (`ToDataJson()`), checksum generation, domain equivalence diffs. |
| **`Merge`** | `snake_case` | Compact | `Ignore` | **Included** | Included | Partial updates & patching (`MergeableExtension.Merge()`) without overwriting target fields with defaults. |
| **`CamelCase`** | `camelCase` | Compact | `IgnoreAndPopulate` | **Included** | Included | Interoperability with external APIs or JavaScript frontends requiring camelCase. |

---

## Detailed Profile Explanations

### 1. `KifaJsonSerializerSettings.Default`
- **Data Handling**:
  - Converts property names to `snake_case`.
  - Omits `null` values, empty collections, and default enum/primitive values.
  - **Includes** all model properties, including `[ExternalProperty]`.
  - Outputs a single-line compact JSON string.
- **When Used**:
  - General data transfer and JSON round-tripping throughout the application.
  - `KifaServiceRestClient` sending and receiving HTTP RPC payloads.
  - `CloneableExtension.Clone()` for in-memory deep copying.

### 2. `KifaJsonSerializerSettings.Pretty`
- **Data Handling**:
  - Formatted multi-line JSON with 2-space indentation and ordered property layout.
  - **Includes** all model properties, including `[ExternalProperty]`.
- **When Used**:
  - **ASP.NET Web API Output Formatter**: Configured in [`Startup.cs`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Web.Api/Startup.cs) for all controller responses, ensuring clients requesting data over the HTTP API receive complete models (including external data fields like HTML/text content).
  - **Helper Methods**: [`JsonExtensions.ToPrettyJson()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/JsonExtensions.cs).

### 3. `KifaJsonSerializerSettings.Disk`
- **Data Handling**:
  - Configures `IgnoreExternalProperties = true` and `Formatting = Indented`.
  - Suppresses properties adorned with `[ExternalProperty]` so they are omitted from the main `.json` file on disk.
- **When Used**:
  - **Local Storage**: Storing `.json` entity files on disk in [`KifaServiceJsonClient`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Web.Api/KifaServiceJsonClient.cs), where external properties are stored in separate sidecar files (`.html`, `.txt`, etc.).
  - **Layout Verification**: Checking if existing on-disk `.json` files match the expected storage layout in `Get(rewrite: true)`.
  - **Helper Methods**: [`JsonExtensions.ToDiskJson()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/JsonExtensions.cs) and [`JsonExtensions.FromDiskJson()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/JsonExtensions.cs).

### 4. `KifaJsonSerializerSettings.DataContent`
- **Data Handling**:
  - Sets `IgnoredProperties = ["Metadata", "$metadata"]` in `OrderedContractResolver`.
  - Excludes system metadata (such as timestamps, version IDs, freshness flags, status, linking records) from the JSON string.
  - Includes all domain data, including external property content.
- **When Used**:
  - **Content Hashing**: [`JsonExtensions.ToDataJson()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/JsonExtensions.cs) to generate canonical hashes (e.g., SHA256) of domain data.
  - **Data Equality**: Verifying whether core entity content has changed, independent of metadata modifications.

### 5. `KifaJsonSerializerSettings.Merge`
- **Data Handling**:
  - Configures `DefaultValueHandling = DefaultValueHandling.Ignore`.
  - When deserializing into an existing object via `JsonConvert.PopulateObject()`, missing JSON fields are ignored and do not reset existing values to type defaults.
- **When Used**:
  - **Object Merging**: [`MergeableExtension.Merge()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/MergeableExtension.cs) for combining partial records or applying delta patches.

### 6. `KifaJsonSerializerSettings.CamelCase`
- **Data Handling**:
  - Uses `CamelCaseNamingStrategy` instead of `SnakeCaseNamingStrategy`.
- **When Used**:
  - Interacting with external REST APIs, third-party services, or web clients expecting standard camelCase formatting.
