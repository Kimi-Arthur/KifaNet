# JSON Serialization & ExternalProperty Optimization

## Overview
This document outlines the architectural enhancements to JSON serialization, metadata suppression, and external property management across `Kifa`, `Kifa.Service`, and `Kifa.Web.Api`.

## Key Improvements

### 1. Hierarchical `ShouldSerialize` Pattern
Previously, `DataMetadata` used a custom `IsEmpty` boolean check and `CleanupForWriting` had to manually prune `Linking`, empty collections, and metadata sub-objects before serializing to disk.
- Json.NET natively supports method-level convention matching (`ShouldSerialize<PropertyName>()` and `ShouldSerialize()`).
- The serialization pipeline now leverages this idiomatically:
  - [`DataModel.ShouldSerializeMetadata()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataModel.cs): returns `true` if `Metadata?.ShouldSerialize() == true`.
  - [`DataMetadata.ShouldSerialize()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataMetadata.cs): evaluates `ShouldSerializeLinking() || Status != DataStatus.OK || Version != null || ShouldSerializeLastRefreshed() || Overrides.Count > 0`.
  - [`LinkingMetadata.ShouldSerialize()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataMetadata.cs): returns `true` only when `Target != null || ShouldSerializeLinks() || ShouldSerializeVirtualLinks()`.
  - Sub-properties declare explicit `ShouldSerializeLinking()`, `ShouldSerializeLinks()`, and `ShouldSerializeVirtualLinks()`, while non-nullable collections like `Overrides` are automatically handled by `OrderedContractResolver`.

### 2. Standalone Collection Suppression in `OrderedContractResolver`
- Generalized non-nullable collection suppression in [`OrderedContractResolver`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/OrderedContractResolver.cs) across all collection types (`ICollection`, `IDictionary`, `IEnumerable`).
- Non-nullable collections (where `new()` is the default unpopulated state) are suppressed when `Count == 0`.
- Nullable collection properties (`List<T>?`, `SortedSet<T>?`, `Dictionary<K, V>?`) preserve explicitly initialized empty instances (`new()`) to allow intentional `[]` / `{}` serialization.

### 3. Native `[ExternalProperty]` Serialization Suppression & `Disk` Profile
- [`ExternalPropertyAttribute`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/ExternalPropertyAttribute.cs) is recognized directly by [`OrderedContractResolver`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/OrderedContractResolver.cs) via the `IgnoreExternalProperties` flag.
- [`KifaJsonSerializerSettings.Disk`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/KifaJsonSerializerSettings.cs) sets `IgnoreExternalProperties = true` for on-disk persistence, while `Default` and `Pretty` include external properties for RPC and Web API output formatters.
- [`CloneableExtension.Clone()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/CloneableExtension.cs) uses `Default` settings so cloned instances retain external property values.

### 4. Non-Mutating `KifaServiceJsonClient`
- `WriteTarget` and `CleanupForWriting` no longer destructively nullify metadata or set `property.SetValue(data, "")` in-memory.
- Data structures remain intact and valid in memory after write operations.
