---
name: release
description: Auto-advance 3-part project versions (MAJOR for new commands, MINOR for new options/flags, PATCH for fixes/improvements), confirm details with the user, commit, and publish binaries.
---

# Release Automation Skill

Automate version incrementing, git commits, and package/tool publishing following KifaNet's 3-part versioning rules (`MAJOR.MINOR.PATCH`).

## 3-Part Versioning Rules

* **MAJOR (`X.0.0`)**: Incremented when a **new command** (new CLI verb/class) is added or breaking changes are introduced.
* **MINOR (`X.Y.0`)**: Incremented when a **new option or flag** is added to an existing command.
* **PATCH (`X.Y.Z`)**: Incremented for **other improvements, refactorings, UX refinements, or bug fixes**.

## Workflow Steps

1. **Analyze Uncommitted & Recent Changes**:
   * Inspect uncommitted files using `git status` and `git diff`.
   * Identify the target project `.csproj` (e.g. `src/Kifa.Tools.FileUtil/Kifa.Tools.FileUtil.csproj`).
   * Extract current `<Version>` from the `.csproj` file.

2. **Categorize Change Type & Propose Version Bump**:
   * Inspect diffs and added files to categorize the change:
     * New `[Verb]` or new command class $\rightarrow$ **MAJOR** bump (`X.0.0`).
     * New `[Option]` attribute or new CLI flag $\rightarrow$ **MINOR** bump (`X.Y.0`).
     * Other bug fixes, performance improvements, or refactoring $\rightarrow$ **PATCH** bump (`X.Y.Z`).
   * Calculate proposed `<new_version>`.

3. **Draft Release Commit Message**:
   * Follow format: `release(<tool_name> <new_version>): <concise description of main changes>`
   * Example: `release(filex 5.6.4): interactive multi-source file linking`

4. **Prompt User for Confirmation**:
   * Present release details formatted as **one information item per line**:
     * **Tool**: `<tool_name>`
     * **Target Project**: `<path_to_csproj>`
     * **Change Type**: `<MAJOR | MINOR | PATCH>` (`<reason>`)
     * **Version Bump**: `<current_version>` $\rightarrow$ `<new_version>`
     * **Commit Message**: `release(<tool_name> <new_version>): <description>`
   * Always ask for explicit user confirmation on version and release details before updating files, committing, or publishing.

5. **Execute Version Bump, Commit & Publish**:
   * Update `<Version>X.Y.Z</Version>` in the target `.csproj`.
   * Stage modified files and commit with the confirmed message (`git add . && git commit -m "..."`).
   * Run release publication script (e.g., `./scripts/publish.sh <path_to_csproj>`).
   * Verify output and report publication status to the user.
