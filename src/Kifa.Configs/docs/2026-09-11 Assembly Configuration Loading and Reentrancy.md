# Assembly Configuration Loading and Reentrancy

## Overview
[`KifaConfigs`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Configs/KifaConfigs.cs) is the centralized configuration engine in KifaNet. It binds hierarchical YAML configuration files (specified by the `KIFA_CONFIG` environment variable or `--config` CLI argument) to static properties across KifaNet assemblies.

This document describes the configuration discovery mechanism, the Mono runtime crash caused by recursive loader re-entrancy, the architectural design of the safe loading loop, and the concurrency guarantees.

---

## Configuration Discovery Mechanism

Configuration properties in KifaNet are static properties on classes within the `Kifa` or `Mito` namespaces (e.g. `Kifa.Api.Files.KifaFile`, `Kifa.Cloud.BaiduCloud.BaiduCloudStorageClient`).

### Discovery & Binding Flow
1. **Assembly Filtering**: Only assemblies whose names start with `Kifa` or `Mito` are inspected (`ShouldProcessAssembly`). BCL assemblies (`System.*`, `Microsoft.*`) and third-party libraries (`YamlDotNet`, `NLog`, `CommandLine`, etc.) are skipped.
2. **Property Extraction**: Scans types in matching assemblies for public static properties with setters.
3. **YAML Deserialization & Assignment**: Parses the YAML configuration hierarchy and deserializes values into the target property types using `YamlDotNet`.

---

## The Re-entrancy Problem & Mono Runtime Crash

### The Issue
Prior to the fix, [`KifaConfigs.Init`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Configs/KifaConfigs.cs) subscribed to `AppDomain.CurrentDomain.AssemblyLoad` before running its initial configuration scan:

```
Init()
  │
  ├──> Subscribe to AppDomain.CurrentDomain.AssemblyLoad
  │
  └──> LoadFromSystemConfigs()
         └──> GetAllProperties()
                └──> Assembly.GetTypes() / Type.GetProperties()
                       │
                       └──> Runtime resolves referenced assembly (e.g. Kifa.Api)
                              │
                              └──> Fires AssemblyLoad event synchronously
                                     │
                                     └──> Re-enters LoadFromSystemConfigs(newAssembly)
                                            └──> GetProperties() -> Assembly.GetTypes()
                                                   └──> Fires another AssemblyLoad event... (Recursion!)
```

### Why It Crashed with Native `SIGABRT` on Android (Termux)
* **Mono Runtime (`mono_loader_lock`)**: On Android (Termux), .NET runs on the Mono runtime. Mono holds a native global loader lock (`mono_loader_lock`) while dispatching `AssemblyLoadContext:OnAssemblyLoad` / `AppDomain.AssemblyLoad` event notifications.
* When managed code inside the event handler called back into native reflection (`InternalGetTypes`, `GetPropertiesByName_native`), Mono detected re-entrant loader lock acquisition and uncommitted metadata state, triggering an assertion failure in native code (`g_error()` / `abort()`) and terminating the process with **`SIGABRT`**.
* **CoreCLR (macOS / Linux)**: CoreCLR uses a fine-grained, multi-phase loader architecture that releases internal locks before firing managed load events. As a result, CoreCLR tolerated the recursion without crashing (although it still performed redundant disk I/O and YAML parsing).

---

## Safe Loading Architecture

To eliminate re-entrancy while guaranteeing that all dynamically loaded assemblies are properly configured, [`KifaConfigs`](file:///Users/jingbian/Projects/KifaNet/src/Kifa.Configs/KifaConfigs.cs) implements a reentrancy-guarded iterative batch loop:

```csharp
public static void LoadFromSystemConfigs(Assembly? assembly = null) {
    lock (ConfigLock) {
        if (isConfiguring) {
            return;
        }

        if (assembly != null && (!ShouldProcessAssembly(assembly) || ProcessedAssemblies.Contains(assembly))) {
            return;
        }

        isConfiguring = true;
        try {
            while (true) {
                var newAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !ProcessedAssemblies.Contains(a) && ShouldProcessAssembly(a))
                    .ToList();
                if (newAssemblies.Count == 0) {
                    break;
                }

                foreach (var a in newAssemblies) {
                    ProcessedAssemblies.Add(a);
                }

                var properties = new Dictionary<string, PropertyInfo>();
                foreach (var a in newAssemblies) {
                    foreach (var property in GetProperties(a)) {
                        properties[property.Key] = property.Value;
                    }
                }

                if (properties.Count > 0) {
                    var assemblyNames = string.Join(", ", newAssemblies.Select(ass => ass.FullName));
                    Log($"Configure the following {properties.Count} properties in {assemblyNames}:");
                    foreach (var property in properties) {
                        Log($"\t{property.Key}");
                    }

                    LoadConfig(ConfigFilePath, properties);
                }
            }
        } finally {
            isConfiguring = false;
        }
    }
}
```

### Key Components

1. **Re-entrancy Guard (`isConfiguring`)**:
   - When reflection causes the runtime to load a referenced assembly, the synchronous `AssemblyLoad` event handler encounters `if (isConfiguring) return;` and exits immediately without executing reflection inside the loader callback.
2. **Iterative Transitive Discovery (`while (true)`)**:
   - Instead of recursing, the outer loop re-queries `AppDomain.CurrentDomain.GetAssemblies()` on the next iteration. Any transitive assemblies loaded during the prior pass are discovered and configured in a clean batch.
3. **Assembly Deduplication (`ProcessedAssemblies`)**:
   - A `HashSet<Assembly>` ensures each assembly is scanned and configured at most once.
4. **Assembly Name Filtering (`ShouldProcessAssembly`)**:
   - Restricts scanning to `Kifa*` and `Mito*` assemblies, preventing unnecessary type scanning across BCL and third-party packages.
5. **Safe Reflection (`ReflectionTypeLoadException`)**:
   - Safely catches `ReflectionTypeLoadException` to handle optional dependencies without throwing.
6. **Empty Property Check**:
   - `LoadConfig` is only invoked when `properties.Count > 0`, avoiding disk reads and YAML parsing when an assembly contains no configurable static properties.
7. **Idempotent Subscription (`assemblyLoadHooked`)**:
   - Ensures `AppDomain.CurrentDomain.AssemblyLoad` is subscribed only once across multiple `Init()` calls.

---

## Concurrency & Thread-Safety Guarantees

Both `lock (ConfigLock)` and `isConfiguring` are necessary to handle both same-thread re-entrancy and multi-threaded execution:

| Mechanism | Scope | Purpose |
|---|---|---|
| `isConfiguring` | Same thread | Acts as a re-entrancy token. When `AssemblyLoad` fires on the same thread during reflection, it exits immediately to avoid Mono loader lock recursion. |
| `lock (ConfigLock)` | Cross thread | Ensures mutual exclusion so concurrent threads loading assemblies do not corrupt `ProcessedAssemblies` or apply configs to static properties concurrently. |

### Execution Scenarios

#### Scenario 1: Same-Thread Transitive Assembly Loading
1. Thread A enters `LoadFromSystemConfigs()`, acquires `ConfigLock`, and sets `isConfiguring = true`.
2. While inspecting Assembly 1, the runtime loads Assembly 2.
3. `AssemblyLoad` fires on Thread A $\rightarrow$ calls `LoadFromSystemConfigs(Assembly 2)`.
4. The re-entrant call sees `isConfiguring == true` and returns immediately (no reflection inside the loader lock).
5. Thread A completes the current loop iteration and re-queries `AppDomain.CurrentDomain.GetAssemblies()`.
6. Assembly 2 is found (not yet in `ProcessedAssemblies`), and configured in the next iteration.

#### Scenario 2: Concurrent Multi-Threaded Loading
1. Thread A is actively configuring assemblies inside `ConfigLock`.
2. Thread B loads an assembly dynamically and calls `LoadFromSystemConfigs()`.
3. Thread B blocks on `lock (ConfigLock)` (it does **not** skip execution).
4. Thread A completes, sets `isConfiguring = false`, and releases the lock.
5. Thread B acquires `ConfigLock`, discovers any unconfigured assemblies (if not already handled by Thread A), and configures them.
