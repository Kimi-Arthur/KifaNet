File Utility
===

Package
---
**Kifa.Tools.FileUtil**

[![Nuget](https://img.shields.io/nuget/v/Kifa.Tools.FileUtil.svg)](http://nuget.org/packages/Kifa.Tools.FileUtil)

Commands
---
- `filex add <file_url>`
  - `-f` `--force-check` Check file integrity even if it is already recorded.
- `filex upload`
- `filex get`
- `filex ln`
- `filex rm`
- `filex ls`
- `filex clean`
- Other notes
  - `<file_url>` can be of the following formats
    - `baidu:Account/path/to/file`
    - `local:wd0/path/to/file`
    - `/absolute/system/path/to/file`
    - `../relative/path/to/file`
    - `C:\windows\path\to\file`

Dependencies:
---
- **CommandLineParser**: 2.3.0
- **Kifax.Api.Files**
- **Kifax.Configs**
- **Kifax.IO**

Updates:
---
### 1.0
- Added `fileutil cp` etc.
- 1.1
  - Do precheck based on FileInformation's status.

### 2.0
- Abandoned old style commands. Use safer ones.
- Added `fileutil upload` etc.
- 2.1
  - Added `fileutil get`
- 2.2
  - Added `fileutil ln`
- 2.3
  - Support folder in `fileutil rm`
- 2.4
  - Allow removal of file instances with different name (like linked file, uploaded file).
- 2.5
  - Rename to `f` as binary name.
- 2.6
  - Add quick mode for `fileutil upload` with flag `-q`, which skips verification of destination.
- 2.7
  - Add `fileutil touch`

### 3.0
- Added `filex clean`.

### 4.0
- Added `filex import`.

### 5.0
- Added `filex check`.
- 5.1
  - Added `-o` in `filex add` to allow overwriting original data.

### 6.0
- Added `filex trash` to move files to some .Trash folder for cleanness.

### 7.0
- Upgrade to dotnet core 3.0.

### 8.0
- Added `filex rm-empty` to recursively remove empty folders in file system.

### 9.0
- Added `filex decode` to extract/process games files.

### 12.0
- Added `filex refresh` to refresh resources.
