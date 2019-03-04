Subtitle Utility Application
===

Assembly Name
---
**subutil**

Commands
---
- `subutil bilibili`
- `subutil generate`
- `subutil fix`
- `subutil rename`

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Pimix.Apps.SubUtil.svg)](http://nuget.org/packages/Pimix.Apps.SubUtil)

Dependencies:
---
- **CommandLineParser**: 2.3.0
- **Pimix.Api.Files**
- **Pimix.Bilibili**
- **Pimix.Configs**
- **Pimix.Service**

Updates:
---
### 1.0
- Added `subutil comments` to get comments from Bilibili.
- 1.1
  - Renamed `subutil comments` to `subutil bilibili`.
- 1.2
  - Added `subutil generate` to generate ASS subtitle.
- 1.3
  - Added `subutil fix` to fix ASS subtitle, like resize to 1080p.
- 1.4
  - Added `subutil rename` to rename video downloaded from biliplus to standard name.
- 1.5
  - Renamed to `subx`.
  - Support multiple-P videos.
- 1.6
  - Direct `subx generate`'s input and output to /Subtitles folder.