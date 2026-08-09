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
     * New `[Verb]` or new command class -> **MAJOR** bump (`X.0.0`).
     * New `[Option]` attribute or new CLI flag -> **MINOR** bump (`X.Y.0`).
     * Other bug fixes, performance improvements, or refactoring -> **PATCH** bump (`X.Y.Z`).
   * Calculate proposed `<new_version>`.

3. **Handle Pending Changes (Two-Commit Workflow)**:
   * **If there are pending code changes**:
     * Split the release process into two separate, sequential commits:
       1. **First Commit (Functional Changes)**: Stage and commit the functional changes first. Follow the **Selective Commit Skill** for this step, using standard conventional commit formats (e.g. `feat(filex): ...` or `fix(filex): ...`).
       2. **Second Commit (Version Bump & Release)**: Perform the version bump in the `.csproj` file. Stage and commit the version bump using the `release` prefix with the detailed description of the release content (e.g. `release(filex 5.6.4): interactive multi-source file linking`).
   * **If there are no pending changes**:
     * Proceed directly to the version bump and release as a single commit using the `release` prefix and detailed description of the changes being released.

4. **Draft Release Commit Message**:
   * Follow format: `release(<tool_name> <new_version>): <detailed description of the release content/changes>`
   * Example: `release(filex 5.6.4): interactive multi-source file linking`

5. **Prompt User for Confirmation**:
   * Present release details formatted as **one information item per line**:
     * **Tool**: `<tool_name>`
     * **Target Project**: `<path_to_csproj>`
     * **Change Type**: `<MAJOR | MINOR | PATCH>` (`<reason>`)
     * **Version Bump**: `<current_version>` -> `<new_version>`
     * **Commit Message**: `release(<tool_name> <new_version>): <description>`
   * Always ask for explicit user confirmation on version and release details before updating files, committing, or publishing.

6. **Execute Version Bump, Commit & Publish**:
   * Update `<Version>X.Y.Z</Version>` in the target `.csproj`.
   * Stage the modified `.csproj` and commit with the confirmed message (`git add <path_to_csproj> && git commit -m "..."`).
   * Run release publication script (e.g., `./scripts/publish.sh <path_to_csproj>`).
   * Verify output and report publication status to the user.
