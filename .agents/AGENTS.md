# Customization Rules for KifaNet

## Baidu Cloud Migration Rules
- During the migration of Baidu Cloud (or other PCS/Cloud APIs) from JToken-based responses to structured RPC response models, pay close attention to fields containing collections of items (like `entries` in file lists or diff lists). Some APIs return a JSON object/map (dictionary) where the keys are paths or IDs, rather than a JSON array (list). Ensure these are deserialized as `Dictionary<string, T>` and not `List<T>`.
## Release & Git Rules
- Strictly DO NOT update project versions, publish packages to NuGet, or perform `git commit`/`git push` unless explicitly requested by the user in that specific query; a commit/release request applies strictly to the single turn in which it was asked and NEVER carries over to subsequent turns.

## Git Commit Rules
- Use meaningful, concise commit messages following conventional commit standards.
- Keep commits focused and atomic.

## Code Structure & Style Rules
- Place `const` values (e.g., string/numeric constants) and helper `static` fields (such as `Regex` instances or pattern constants) together just above where they are used (or above the first method using them), rather than placing all constants/statics at the top of the class.
- Common class utility statics (such as `Logger`, `HttpClient`, or service client instances) should remain at the top of the class.

## Temporary Files Rules
- Always place all temporary files, crawler state/progress files, local logs, intermediate scripts, or certificates generated during agent operations in the `.agent_temp/` directory.
- Do not write temporary or untracked files to the root directory of the workspace or other source/test folders.

## Code Modification Rules
- Automatically apply code edits and file modifications directly without prompting for pre-approval. The user reviews changes with external tools.
