# Customization Rules for KifaNet

## Baidu Cloud Migration Rules
- During the migration of Baidu Cloud (or other PCS/Cloud APIs) from JToken-based responses to structured RPC response models, pay close attention to fields containing collections of items (like `entries` in file lists or diff lists). Some APIs return a JSON object/map (dictionary) where the keys are paths or IDs, rather than a JSON array (list). Ensure these are deserialized as `Dictionary<string, T>` and not `List<T>`.
## Release & Versioning Rules
- Do not automatically update version or publish package/tools to NuGet. Only perform publish when explicitly requested by the user.
- Do not automatically commit or push git changes unless explicitly requested by the user.

## Git Commit Rules
- Use meaningful, concise commit messages following conventional commit standards.
- Keep commits focused and atomic.

## Temporary Files Rules
- Always place all temporary files, crawler state/progress files, local logs, intermediate scripts, or certificates generated during agent operations in the `.agent_temp/` directory.
- Do not write temporary or untracked files to the root directory of the workspace or other source/test folders.
