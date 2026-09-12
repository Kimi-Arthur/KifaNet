# KifaNet C# Code Style Guide

This style guide documents the formatting, syntax, structure, and design conventions of the KifaNet codebase. It reflects the existing codebase patterns and JetBrains Rider configuration.

---

## 1. Bracing and Layout (K&R / 1TBS End-of-Line Style)

* **Opening Braces `{`**:
  * Always placed at the end of the line preceding the block (same line), NOT on a new line.
  * Applies to: namespaces, class/struct/interface/record declarations, methods, properties, constructors, control flow blocks (`if`, `for`, `foreach`, `while`, `switch`), try/catch/finally blocks, and object/collection initializers.
  ```csharp
  public class ExampleService {
      public void ProcessItem(string name) {
          if (name != null) {
              Execute();
          } else {
              Fallback();
          }
      }
  }
  ```

* **Control Flow Continuation (`else`, `catch`, `finally`)**:
  * Placed on the same line as the closing brace: `} else {`, `} catch (Exception ex) {`, `} finally {`.

* **Control Flow Braces Requirement**:
  * Braces are mandatory for `if`, `else`, `for`, `foreach`, `while`, and `do` blocks even for single statements. Never use dangling unbraced statements.

* **Indentation & Spacing**:
  * Standard 4 spaces per indentation level. Tabs are not used.
  * Space after typecast: `(int) value`.
  * Space before and after binary operators (`+`, `-`, `==`, `!=`, `??`).
  * Single trailing newline at the end of every file; trim all trailing whitespace on each line.
  * Maximum recommended line length is 100 characters.

---

## 2. Imports and Using Directives

* **Namespaces**:
  * Use file-scoped namespaces without indentation:
    ```csharp
    namespace Kifa.Tools;
    ```

* **Ordering of `using` Directives**:
  * `System` and `System.*` namespaces are placed first at the top.
  * Third-party package namespaces (e.g. `Newtonsoft.Json`, `NLog`, `CommandLine`, `YamlDotNet`) follow.
  * Internal/project namespaces (e.g. `Kifa.*`, `Kifa.Service`) follow.
  * Within each category or as a whole, usings are sorted cleanly.
  * No empty lines separating using groups.
  * Remove unused `using` directives.

---

## 3. Class Structure and Member Placement

* **Top of Class (Common Class Utilities)**:
  * Static loggers, shared HTTP clients, and service clients are placed at the top of the class:
    ```csharp
    public class SampleService {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        static readonly HttpClient HttpClient = new();
        public static ServiceClient Client { get; set; } = new RestServiceClient();
        ...
    }
    ```

* **Constants and Helper Statics (`const`, `static Regex`, etc.)**:
  * `const` values (e.g., string/numeric constants) and helper `static` fields (such as `Regex` instances or format patterns) must be placed **immediately above** the first method or property that uses them, rather than clustering all constants/statics at the top of the class.

* **Client Interfaces & Boilerplate**:
  * Data models and services place client definitions inside a `#region Clients` block:
    ```csharp
    #region Clients

    public static ServiceClient Client { get; set; } = new RestServiceClient();

    public interface ServiceClient : KifaServiceClient<SampleModel> {
    }

    public class RestServiceClient : KifaServiceRestClient<SampleModel>, ServiceClient {
    }

    #endregion
    ```

---

## 4. Null Handling and Modern C# Language Features

* **Explicit Null Checks**:
  * Avoid `string.IsNullOrEmpty` or `string.IsNullOrWhiteSpace` when `null` represents the normal default/unset state (e.g. for hash fields like `Sha256`, database IDs, or optional model properties). Use explicit null checks (`== null` or `!= null`) instead.
* **Null Coalescing and Compound Assignment**:
  * Use `??` for fallback values: `var val = a ?? b;`
  * Use `??=` for lazy/conditional assignment: `target.Field ??= value;`
  * Use `?.` for safe navigation: `item?.Property`
* **Target-Typed `new()` and Collection Expressions**:
  * Prefer target-typed new `new()` where the type is obvious from declaration.
  * Prefer collection expressions `[]` and spread `[..items]` for lists/arrays in modern code.
* **Expression-Bodied Members**:
  * Use expression bodies (`=>`) for single-line methods, properties, getters, and operator overloads.

---

## 5. Comments and Documentation

* **Simple Comments (`//`)**:
  * Use simple single-line `//` comments for code explanations and section markers.
  * Do **NOT** use triple-slash (`///`) XML documentation comments or docstrings in code.
* **Doc Location**:
  * Project-level reasoning, planning, and architecture documents belong in a `docs/` folder located inside the relevant project's directory (e.g., `src/Kifa.Web.Api/docs/`).

---

## 6. Proposing Style Changes

* Any proposed modification or divergence from this style guide must be explicitly proposed to the user and approved before being applied to code files.
