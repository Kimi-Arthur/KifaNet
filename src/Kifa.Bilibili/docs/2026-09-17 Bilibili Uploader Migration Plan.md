# Bilibili Uploader Decoupling and Data Migration Plan

## 1. Background & Objectives
Previously, `BilibiliUploader` (`bilibili/uploaders`) stored both uploader profile information (`Name`, `Id`) and the full video feed history (`Aids`, `RemovedAids`). 

This coupled lightweight profile lookups (rarely updated, 30-day cache) with heavy video list synchronizations (frequently updated, 1-day cache).

The objective is to decouple the uploader profile from the video feed by introducing `BilibiliUploaderVideos` (`bilibili/uploader_videos`), mirroring the architecture used in `YouTubeUploader` / `YouTubeUploaderVideos`, while ensuring **zero data loss** for historically deleted videos (`RemovedAids`).

---

## 2. Architecture & Data Model Split

### `BilibiliUploader` (`bilibili/uploaders`)
* **Properties:**
  * `Id` (string, mid)
  * `Name` (string)
* **RefreshInterval:** 30 days (`TimeSpan.FromDays(30)`)
* **`Fill(bool deep = false)`:**
  * Calls `UploaderInfoWebRpc(Id)` to refresh uploader profile and name.

### `BilibiliUploaderVideos` (`bilibili/uploader_videos`)
* **Properties:**
  * `Id` (string, mid)
  * `Aids` (`List<string>`)
  * `RemovedAids` (`List<string>`)
* **RefreshInterval:** 1 day (`TimeSpan.FromDays(1)`)
* **`Fill(bool deep = false)`:**
  * Fetches the video feed incrementally.
  * Calculates set differences to populate and track `RemovedAids`.

---

## 3. Two-Stage Migration Plan

### Stage 1: Dual-Model Introduction & Backward-Compatible Seeding
Because deleted videos no longer exist in Bilibili's public API, `RemovedAids` cannot be re-fetched. Stage 1 ensures seamless data transfer from existing records:

1. **Retain Legacy Properties in `BilibiliUploader`:**
   * Keep `Aids` and `RemovedAids` temporarily on `BilibiliUploader` so existing disk JSON records deserialize cleanly into memory.
   * `BilibiliUploader.Fill()` is updated to only fetch profile metadata.

2. **Automated Seeding in `BilibiliUploaderVideos.Fill()`:**
   * When `BilibiliUploaderVideos.Fill()` runs for an uploader whose in-memory `Aids` and `RemovedAids` are empty, it loads the existing `BilibiliUploader` record via `BilibiliUploader.Client.Get(Id)`.
   * It seeds `Aids = [..legacy.Aids]` and `RemovedAids = [..legacy.RemovedAids]` before executing the incremental update and diff calculation.

3. **Tooling & CLI Updates:**
   * Update `DownloadUploaderCommand` (`bili up <id>`) to retrieve metadata from `BilibiliUploader` and video lists from `BilibiliUploaderVideos`.

### Stage 2: Cleanup & Invalidation
* Once all existing uploader data has been seeded into `bilibili/uploader_videos/`, remove the legacy `Aids` and `RemovedAids` properties from `BilibiliUploader`.
* When `BilibiliUploader` records are rewritten upon their next refresh, the obsolete fields will be dropped from `bilibili/uploaders/{id}.json`.
