# JSON Serialization & ExternalProperty Optimization

## Overview
This document outlines the architectural enhancements to JSON serialization, metadata suppression, and external property management across `Kifa`, `Kifa.Service`, and `Kifa.Web.Api`.

## Key Improvements

### 1. Hierarchical `ShouldSerialize` Pattern
Previously, `DataMetadata` used a custom `IsEmpty` boolean check and `CleanupForWriting` had to manually prune `Linking`, empty collections, and metadata sub-objects before serializing to disk.
- Json.NET natively supports method-level convention matching (`ShouldSerialize<PropertyName>()` and `ShouldSerialize()`).
- The serialization pipeline now leverages this idiomatically:
  - [`DataModel.ShouldSerializeMetadata()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataModel.cs): returns `true` if `Metadata?.ShouldSerialize() == true`.
  - [`DataMetadata.ShouldSerialize()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataMetadata.cs): evaluates `ShouldSerializeLinking() || Status != DataStatus.OK || Version != null || ShouldSerializeLastRefreshed() || ShouldSerializeOverrides()`.
  - [`LinkingMetadata.ShouldSerialize()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Service/DataModel/DataMetadata.cs): returns `true` only when `Target != null` or links collections contain items.
  - Sub-properties like `Overrides` and `Linking` declare explicit `ShouldSerializeOverrides()` and `ShouldSerializeLinking()`.

### 2. Generalized Collection Suppression in `OrderedContractResolver`
- Expanded collection suppression in [`OrderedContractResolver`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/OrderedContractResolver.cs) from strictly `IList<>` and `IDictionary<,>` to all `ICollection`, `IDictionary`, `IReadOnlyCollection<>`, and `ICollection<>` instances.
- Empty collections (`Count == 0`) are automatically omitted without needing individual model annotations.

### 3. Native `[ExternalProperty]` Serialization Suppression
- [`ExternalPropertyAttribute`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/ExternalPropertyAttribute.cs) was moved to `Kifa` (namespace `Kifa.Service`).
- [`OrderedContractResolver`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/OrderedContractResolver.cs) automatically attaches `property.ShouldSerialize = _ => false` to any property adorned with `[ExternalProperty]`.
- [`CloneableExtension.Clone()`](file:///Users/jingbian/Projects/KifaNet/src/Kifa/Json/CloneableExtension.cs) explicitly copies external properties during cloning.

### 4. Non-Mutating `KifaServiceJsonClient`
- `WriteTarget` and `CleanupForWriting` no longer destructively clear `property.SetValue(data, "")` or wipe `Metadata` in-memory.
- Data structures remain intact and valid in memory after write operations.
