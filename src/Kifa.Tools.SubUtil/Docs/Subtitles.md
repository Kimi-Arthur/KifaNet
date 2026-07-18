# Subtitle & Danmaku Architecture and Naming Specification

## Repository & Storage Strategy
* Subtitles and danmaku files are maintained in a **separate Git repository** from the primary video filesystem, with common backup strategies.
* Covers both subtitle files (`.srt`, `.ass`) and danmaku files (`.xml`).

---

## Filename Conventions

Assuming the original video file is named `[Name].mp4`:

### 1. Imported Subtitle Files
* **Format**: `[Name].[languagecode].[ext]` or `[Name].[group].[languagecode].[ext]`
* **File Extensions (`[ext]`)**: `srt`, `ass`
* **Language Codes (`[languagecode]`)**: `en`, `ja`, `zh`, etc.
  * **Rule**: Dual/combined language codes (e.g., `zh-en`) are **not allowed**. Only the main target language code is used.
  * Standard ISO 639 language code references (ISO 639-1 / 639-2).
* **Example**:
  * `咲-Saki- S01E25 全国.华盟字幕社.zh.ass`

### 2. Danmaku Files
* **Format**: `[Name].c[dddd].xml` or `[Name].c[dddd]-[group].xml`
* **Example**:
  * `咲-Saki- S01E25 全国.c123456.xml`
  * `咲-Saki- S01E25 全国.c123456-华盟BD.xml`

### 3. Final Generated Subtitle Files
* **Format**: `[Name].<[danmaku-tag]>.[lang-tag].ass`
* **Danmaku Tag (`<[danmaku-tag]>`)**: Always enclosed within angle brackets `< >`.
  * `bilibili` for official Bilibili danmaku or unknown Bilibili group (e.g., `<bilibili>`).
  * `[group]` for known groups/versions (e.g., `<华盟BD>`).
  * Combined danmaku sources are joined with `+` inside `< >` (e.g., `<华盟DVD+华盟BD>`).
* **Language Tag (`[lang-tag]`)**: Included only when subtitles of that language are available. May include group info if necessary (e.g., `华盟字幕社.zh`).
* **Examples**:
  * `咲-Saki- S01E25 全国.<华盟DVD+华盟BD>.华盟字幕社.zh.ass` (Chinese subtitles + danmaku from 华盟DVD & 华盟BD)
  * `咲-Saki- S01E25 全国.<华盟DVD+华盟BD>.ass` (Danmaku from 华盟DVD & 华盟BD, no subtitles)
  * `咲-Saki- S01E25 全国.<bilibili>.ass` (Danmaku from Bilibili, no subtitles)
