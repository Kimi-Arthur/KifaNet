# Folder Copying Behavior

## Overview
When executing the copy command (`filex cp` or `CopyCommand`), the destination path behavior is determined by explicit syntax and the number of source arguments, rather than whether the destination directory already exists on disk.

## Rules
1. **Single Source without Trailing Slash**:
   - When copying a folder `A` to `B` (`cp A B`), all child contents of `A` are mapped directly to `B` (e.g., `A/file.txt` -> `B/file.txt`).
   - If `B` already exists, running `cp A B` again will still map to `B` (e.g., `A/file.txt` -> `B/file.txt`) and will **not** nest `A` inside `B` as `B/A/file.txt`.
2. **Trailing Slash on Destination**:
   - If the destination explicitly ends with a trailing slash (`cp A B/`), the destination is treated as a container folder, so `A` is nested inside `B` (e.g., `A/file.txt` -> `B/A/file.txt`).
3. **Multiple Sources**:
   - When multiple sources are supplied (`cp A C B`), the destination is treated as a container folder, and all sources are copied into their respective subfolders under `B` (e.g., `A/file.txt` -> `B/A/file.txt`, `C/file2.txt` -> `B/C/file2.txt`).
