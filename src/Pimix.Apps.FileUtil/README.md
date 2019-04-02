File Utility
===

Package
---
**Pimix.Apps.FileUtil**

[![Nuget](https://img.shields.io/nuget/v/Pimix.Apps.FileUtil.svg)](http://nuget.org/packages/Pimix.Apps.FileUtil)

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
- **Pimix.Api.Files**
- **Pimix.Configs**
- **Pimix.IO**

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
