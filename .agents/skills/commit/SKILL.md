---
name: commit
description: Stage a specific set of files and create an atomic git commit with a proper Conventional Commits message.
---

# Selective Commit Skill

Stage a specific subset of files and create a focused, atomic git commit following Conventional Commits format (`<type>(<scope>): <description>`).

## Workflow Steps

1. **Identify Target Files**:
   * Inspect uncommitted files using `git status` and `git diff`.
   * Determine the specific files requested or relevant to the atomic change.
   * Do NOT stage unrelated files.

2. **Inspect File Diffs**:
   * Run `git diff <file1> <file2> ...` to analyze the exact changes made to the specified files.

3. **Draft Conventional Commit Message**:
   * Follow the format: `<type>(<scope>): <concise description>`
   * **Types**:
     * `feat`: A new feature
     * `fix`: A bug fix
     * `refactor`: Code restructuring without changing functionality
     * `style`: Formatting, missing semicolons, prompt text refinements
     * `docs`: Documentation changes
     * `test`: Adding or updating tests
     * `chore`: Maintenance or build tasks
   * **Scope**: Tool or component name (e.g. `filex`, `subx`, `cli`, `core`).
   * **Description**: Imperative, present-tense description (e.g. `add split episode sub-part selection`).

4. **Stage & Commit Selected Files**:
   * Stage ONLY the specified files: `git add <file1> <file2> ...`
   * Commit with the drafted message: `git commit -m "<type>(<scope>): <description>"`

5. **Verify**:
   * Run `git status` to verify that ONLY the target files were committed and remaining files stay uncommitted.
   * Report the commit SHA, message, and committed files to the user.
