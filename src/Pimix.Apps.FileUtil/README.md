File Utility Application
===

Assembly Name
---
**fileutil**

Commands
---
- `fileutil upload`
- `fileutil get`
- `fileutil ln`
- `fileutil add`
- `fileutil rm`
- `fileutil ls`

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.Apps.FileUtil.svg)](http://nuget.org/packages/Pimix.Apps.FileUtil)

Dependencies:
---
- **CommandLineParser**: 2.3.0
- **Pimix.Api.Files**
- **Pimix.Configs**
- **Pimix.IO**

Updates:
---
### 2.0
- Abandoned old style commands. Use safer ones.
- Added `fileutil upload` etc.
- 2.1
  - Added `fileutil get`
- 2.2
  - Added `fileutil ln`
  

### 1.0
- Added `fileutil cp` etc.
- 1.1
  - Do precheck based on FileInformation's status.
