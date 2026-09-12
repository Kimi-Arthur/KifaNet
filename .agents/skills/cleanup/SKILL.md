---
name: cleanup
description: Clean up code (formatting, imports, null coercion, style standards, dead code) across the entire repository or on specific files/directories.
---

# Code Cleanup Skill

Performs comprehensive code cleanup across the entire repository or on specific target files/directories. Enforces KifaNet code style rules, optimizes syntax (null coalescing, modern C# constructs), cleans up imports, and formats whitespace.

## Target Scopes

The cleanup skill can be invoked with a specified scope:
1. **Single File**: e.g., `cleanup src/Kifa/Types/Date.cs`
2. **Directory / Sub-project**: e.g., `cleanup Kifa.YouTube/` or `cleanup src/Kifa.Service/`
3. **Uncommitted / Changed Files**: Files listed in `git status`
4. **Entire Repository**: All C# source files (`**/*.cs`) across the solution

## Cleanup Checklist & Transformations

### 1. Imports & Using Directives
* **Unused Usings**: Automatically remove unused `using` directives using the dedicated cleanup script:
  ```bash
  # For entire solution:
  python3 .agents/skills/cleanup/scripts/remove_unused_imports.py

  # For a specific file or folder:
  python3 .agents/skills/cleanup/scripts/remove_unused_imports.py <path/to/file_or_dir>
  ```
  *(Uses Roslyn IDE0005 analyzer via `dotnet build -warnAsMessage:CS1591 /p:GenerateDocumentationFile=true /p:EnforceCodeStyleInBuild=true --no-incremental`)*
* **Ordering**: Keep `System.*` imports at the top, followed by third-party packages, followed by `Kifa.*` namespaces.
* **Namespaces**: Use file-scoped namespaces (`namespace Kifa.Tools;`) without indentation.
* **Redundancy**: Remove redundant fully qualified type names when the namespace is already imported.

### 2. Null Coalescing & Null Coercion
* Simplify null checks and default fallback assignments:
  * Replace `if (a != null) target.Field = a;` or `target.Field = a != null ? a : target.Field;` with `target.Field ??= a;` or `target.Field = a ?? target.Field;`.
  * Replace `a != null ? a : fallback` with `a ?? fallback`.
  * Replace `obj != null ? obj.Prop : null` with `obj?.Prop`.
* **Important**: Respect KifaNet explicit null rules:
  * Do **not** replace `!= null` with `!string.IsNullOrEmpty(...)` or `!string.IsNullOrWhiteSpace(...)` when `null` explicitly represents the default/unset state. Prefer explicit null checks (`== null` or `!= null`).

### 3. Code Organization & Member Placement
* **Constants and Helper Statics**:
  * Place `const` values (e.g., string/numeric constants) and helper `static` fields (such as `Regex` instances or pattern constants) together **immediately above** the first method using them, rather than placing all constants/statics at the top of the class.
* **Class Utilities**:
  * Common class utility statics (such as `Logger`, `HttpClient`, or service client instances) should remain at the top of the class.

### 4. Formatting & Whitespace
* Indentation: Standard 4 spaces, no tabs.
* Bracing: Opening brace `{` on the same line (end-of-line style); `} catch {`, `} else {`, `} finally {` on the same line.
* Trailing Whitespace: Strip trailing spaces on all lines.
* Blank Lines: Ensure no consecutive multiple blank lines and single trailing newline at end of file.

### 5. Comments & Cleanliness
* Replace triple-slash XML documentation comments (`/// <summary>`) with simple comments (`//`) to stay simple and concise per KifaNet conventions.
* Remove dead code, commented-out debug blocks, and leftover scratch variables.

### 6. Build & Test Verification
* After applying cleanups, always verify the project builds and passes relevant tests:
  * `dotnet build`
  * `dotnet test <test-project> --filter ...`

## Workflow Steps

1. **Determine Scope & Find Target Files**:
   * If a file or folder is provided by the user, resolve the list of matching `.cs` files.
   * If no path is specified, inspect `git status` or scan repository files.
2. **Remove Unused Imports**:
   * Run `python3 .agents/skills/cleanup/scripts/remove_unused_imports.py [target_path]`.
3. **Apply Code & Null Cleanups**:
   * Review target files and apply null coalescing, member placement, and comment formatting rules.
4. **Review Diffs**:
   * Run `git diff <files>` to verify that transformations are accurate and do not introduce unintended semantic changes or style churn.
5. **Build & Test**:
   * Run `dotnet build` (or relevant `dotnet test`) to verify compilation and test passage.
6. **Report Summary**:
   * Summarize the cleaned files, rules applied, and build verification status.
