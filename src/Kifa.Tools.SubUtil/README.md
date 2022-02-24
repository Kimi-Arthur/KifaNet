Subtitle Utility Application
===

Assembly Name
---
**subutil**

Commands
---

- `subx fix`
- `subx generate`
- `subx update`
- `subx clean`

Current Version:
---
[![Nuget](https://img.shields.io/nuget/v/Kifa.Tools.SubUtil.svg)](http://nuget.org/packages/Kifa.Tools.SubUtil)

Dependencies:
---

- **CommandLineParser**: 2.3.0
- **Kifa.Api.Files**
- **Kifa.Bilibili**
- **Kifa.Configs**
- **Kifa.Service**

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

### 2.0

- New way of versioning.
- Added `subx clean` to convert files to UTF-8 with LF line ending.

### 3.0

- Added `subx import` to import subtitles from Sources folder to actual location.