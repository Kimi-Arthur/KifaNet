---
name: commit
description: Clean up files (formatting, imports, null coercion), stage a specific set of files, create an atomic git commit with a proper Conventional Commits message, and push to remote.
---

# Selective Commit Skill

Clean up files, stage a specific subset of files, create a focused, atomic git commit following Conventional Commits format (`<type>(<scope>): <description>`), and push to remote.

## Workflow Steps

1. **Identify Target Files**:
   * Inspect uncommitted files using `git status` and `git diff`.
   * Determine the specific files requested or relevant to the atomic change.
   * Do NOT stage unrelated files.

2. **Clean Up Target Files**:
   * Apply the **[Code Cleanup Skill](file:///Users/jingbian/Projects/KifaNet/.agents/skills/cleanup/SKILL.md)** (`cleanup`) to the target files before committing:
     * **Imports & Usings**: Remove unused `using` statements; sort and organize imports cleanly.
     * **Null Coalescing & Coercion**: Simplify null checks and assignments with `??`, `??=`, and `?.` while preserving explicit null checks where appropriate.
     * **Code Standards & Hygiene**: Position `const` / helper statics above methods using them; keep utility statics at top; use simple comments (`//`); remove leftover debug/dead code.
     * **Formatting & Whitespace**: Ensure standard 4-space indentation, no trailing spaces, and proper file end newlines (`dotnet format` if applicable).
     * **Build & Test Verification**: Run `dotnet build` or relevant tests to verify clean compilation and test passage.

3. **Inspect File Diffs**:
   * Run `git diff <file1> <file2> ...` to analyze the exact changes made to the specified files.

4. **Draft Conventional Commit Message**:
   * Follow the format: `<type>(<scope>): <concise description>`
   * **Types**:
     * `feat`: A new feature
     * `fix`: A bug fix
     * `refactor`: Code restructuring without changing functionality
     * `style`: Formatting, missing semicolons, prompt text refinements
     * `docs`: Documentation changes
     * `test`: Adding or updating tests
     * `chore`: Maintenance or build tasks
   * **Scope**: Tool or component name (e.g. `filex`, `subx`, `ytbx`, `cli`, `core`).
   * **Description**: Imperative, present-tense description (e.g. `add split episode sub-part selection`).

5. **Stage & Commit Selected Files**:
   * Stage ONLY the specified files: `git add <file1> <file2> ...`
   * Commit with the drafted message: `git commit -m "<type>(<scope>): <description>"`

6. **Push to Remote**:
   * Push the commit to the remote repository: `git push`

7. **Verify & Report**:
   * Run `git status` to verify that ONLY the target files were committed and pushed, and remaining files stay uncommitted.
   * Report the commit SHA, message, committed files, and push status to the user.
