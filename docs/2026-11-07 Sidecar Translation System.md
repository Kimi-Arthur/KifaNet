# Sidecar Translation System

This document describes the design for multi-language data overlays in `Kifa.Service` and `Kifa.Web.Api`, replacing the legacy in-file `Translatable<T>` (`$translations`) interface with isolated sidecar files (`{id}.${lang}.json`).

---

## 1. Why Replace `Translatable<T>` (`$translations`)?

Previously, `Translatable<T>` embedded a `[JsonProperty("$translations")] Dictionary<string, T>? Translations` property inside the model class itself:
1. **Schema Pollution**: Every nested class in a hierarchy (e.g., `GermanEntry`, `PhraseEntry`, `EntryExample`, `EtymologySegment`) had to implement `Translatable<T>` and carry a `Translations` dictionary property.
2. **Base File Bloat**: Default requests that only needed the primary language still had to read, parse, and strip all embedded `$translations` blocks from disk.
3. **Coupled Versioning**: Adding or updating a secondary translation (e.g., `zh`) modified the primary `.json` file and bumped its content `$metadata.version`.
4. **Shallow Merging Only**: `TranslatableExtension.GetTranslated<T>()` used `JsonConvert.PopulateObject` with `ObjectCreationHandling.Replace`, which could not merge nested lists/trees without wiping out sibling fields.

**Conclusion**: We **do not need the `Translatable<T>` interface**. Every `DataModel` can support sidecar translations natively at the `Kifa.Service` layer without any model-level boilerplate.

---

## 2. Core Design: Sidecar Files & `translated` Metadata

### 2.1 File Naming Convention: `{id}.${lang}.json`
- **Base File**: `{DataFolder}/{ModelId}/{id}.json` (e.g., `Languages/german/words/Bank.json`)
  - Stores the canonical record in the model's default target language (e.g., `en` for `GermanWord`).
- **Translated Sidecar File**: `{DataFolder}/{ModelId}/{id}.${lang}.json` (e.g., `Languages/german/words/Bank.$zh.json`)
  - Uses KifaNet's reserved `$` prefix on the language suffix (`.$zh.json`) so it never collides with item IDs containing dots (such as `z. B..json` vs `z. B..$zh.json`) and remains valid `.json` for IDE syntax highlighting.
  - `KifaServiceJsonClient.List()` ignores `*.$*.json` when enumerating primary items.
  - Contains a **sparse `TDataModel` tree** with `"id": "{id}.${lang}"` and only the translated fields populated (no `$metadata` stored on disk in the sidecar file).

### 2.2 `Translations` and `Translated` in `DataMetadata` and `KifaDataOptions`
To avoid ambiguity with an item's source/subject language, both the query parameter and `$metadata` fields use **`translations`** / **`translated`** (typed with [`Kifa.Language`](../src/Kifa/Cultures/Language.cs)):

1. **`DataMetadata`** ([`DataMetadata.cs`](../src/Kifa.Service/DataModel/DataMetadata.cs)):
   ```csharp
   // Languages that have sidecar translation files for this item (stored on the base item).
   public SortedSet<Language>? Translations { get; set; }

   // Target language when this instance is a sidecar translation overlay or translated view.
   public Language? Translated { get; set; }
   ```
   - In the base file (`Bank.json`): `Translations` lists available sidecar languages (`"$metadata": { "translations": ["zh"] }`), allowing $O(1)$ lookup without scanning directories on disk, while `Translated` is `null`.
   - In the sidecar file on disk (`Bank.$zh.json`): `$metadata` is omitted (`null`), since the language is already encoded in `"id": "Bank.$zh"`.
   - In the response returned in memory when `?translated=zh` (or direct `Get("Bank.$zh")`) is requested: `"$metadata": { "translated": "zh" }`.

2. **`KifaDataOptions`** ([`KifaDataOptions.cs`](../src/Kifa.Service/KifaDataOptions.cs)):
   ```csharp
   public Language? Translated { get; set; }
   ```
   - Serializes as `?translated=zh` in `GetUrlParameters()`.

3. **Optional `DefaultLanguage` on `WithModelId<T>`**:
   ```csharp
   public static virtual Language? DefaultLanguage => null;
   ```
   - If a caller requests `?translated=en` on `GermanWord` (where `DefaultLanguage => Language.English`), `KifaServiceJsonClient` knows `"en"` is already the base file and returns `Bank.json` directly.

---

## 3. Recursive Tree Merging (`TranslatableExtension.MergeTranslation`)

When `GET /api/{ModelId}/{id}?translated=zh` is called:
1. Read base `TDataModel` from `{id}.json`.
2. Read sparse translation overlay from `{id}.$zh.json` (if it exists).
3. Merge the sparse `JObject` tree onto the base `JObject` tree via [`TranslatableExtension.MergeTranslation()`](../src/Kifa.Service/DataModel/TranslatableExtension.cs):
   - **Scalar / Object Properties**: Non-null properties in the translation tree overwrite the corresponding properties on the base object (e.g., `meaning`, `usage_note`, `translation`, `translation_source`).
   - **Arrays (`JArray`)**:
     - **ID-Keyed Elements** (array items are `JObject`s with an `"id"` field, such as `lexemes` `"Bank#1"` or `entries` `"Bank#1.1"`): Match overlay elements to base elements by `"id"` and recursively merge. Unmatched base elements remain untouched.
     - **Positional Elements** (array items without `"id"`, such as `examples[i]` or `phrases[i]`): Match by index `i` and recursively merge.
   - **Metadata**: Set `$metadata.translated = "zh"` on the merged result while preserving the base record's content `$metadata.version` and `linking`.

### 3.1 Read, Write, Link, and Delete Lifecycle in `KifaServiceJsonClient`
- **Locking (`GetLock`)**:
  - Sidecar IDs (`Bank.$zh`) normalize to their `baseId` (`Bank`) when acquiring locks so `Bank.$zh` and `Bank` always share the same lock.
- **`Get(id, options)` & `List(folder, recursive, options)`**:
  - Requesting `Get("Bank", new KifaDataOptions { Translated = Language.Chinese })` returns the merged `"Bank"` object with `Metadata.Translated = Language.Chinese`.
  - Requesting `Get("Bank.$zh")` directly returns the standalone sparse translation sidecar (with `Id = "Bank.$zh"` and `Metadata.Translated = Language.Chinese`).
  - `List()` filters out `*.$*.json` sidecar files so they are never returned as standalone items.
  - Inside `Read(id, checkId, language)`, for canonical target items (`data.Metadata?.Linking?.Target == null`), both external properties (`ReadAndFillExternalProperties`) and sidecar translations (`ReadAndFillTranslation`, when `language != null && language != TDataModel.DefaultLanguage && data.Metadata?.Translations?.Contains(language) == true`) are loaded in Pass 1 *before* link resolution in Pass 2. Linked items in `List()` simply clone the already-translated target item (`item.Clone()`).
- **`Set(data)` & `Update(data)` via `{id}.${lang}`**:
  - When `data.Id` is a sidecar ID (`Bank.$zh`), `Set()` registers `"zh"` in `Metadata.Translations` on `{RealId}.json` and writes the sparse translation model directly to `{RealId}.$zh.json` (with `"id": "{RealId}.$zh"` and no `$metadata` on disk) without overwriting `{RealId}.json` or invoking upstream `Fill()`.
  - `Update()` with `data.Id = "Bank.$zh"` reads any existing `{RealId}.$zh.json`, merges the incoming translation fields via `MergeTranslation()`, and writes the updated sidecar file.
- **Links & Deletion**:
  - Linked aliases (`Bank_alias.$zh`) resolve `baseItem.RealId` (`Bank`), sharing the canonical target's sidecar translation files.
  - Calling `Delete("Bank.$zh")` deletes only `{RealId}.$zh.json` and removes `"zh"` from `Metadata.Translations` on `{RealId}.json`.
  - If a primary target is deleted (`Delete("Bank")`) and promoted to its next linked ID, any `{id}.$*.json` sidecar files are automatically migrated to `{nextId}.$*.json`.
  - When the final item is deleted, all associated `{id}.$*.json` sidecar files are removed alongside `{id}.json`.

---

## 4. Concrete Example

### Base File (`Bank.json`)
```json
{
  "id": "Bank",
  "$metadata": {
    "translations": [
      "zh"
    ],
    "version": "20261107000000000000"
  },
  "lexemes": [
    {
      "id": "Bank#1",
      "type": "noun",
      "gender": "feminine",
      "pronunciation": "baŋk",
      "key_form": "¨-e",
      "entries": [
        {
          "id": "Bank#1.1",
          "meaning": "bench, park bench",
          "usage_note": "long seat for several people",
          "translation_source": "cambridge",
          "examples": [
            {
              "text": "Wir sitzen auf einer Bank im Park.",
              "translation": "We are sitting on a bench in the park.",
              "translation_source": "gemini-4-argon",
              "source_id": "goethe:die_Bank_1#ex0"
            }
          ]
        }
      ]
    }
  ]
}
```

### Sidecar File (`Bank.$zh.json`)
```json
{
  "id": "Bank.$zh",
  "lexemes": [
    {
      "id": "Bank#1",
      "entries": [
        {
          "id": "Bank#1.1",
          "meaning": "长椅，公园长椅",
          "usage_note": "供多人坐的长凳",
          "translation_source": "gemini-4-argon",
          "examples": [
            {
              "translation": "我们坐在公园的长椅上。",
              "translation_source": "gemini-4-argon"
            }
          ]
        }
      ]
    }
  ]
}
```

### Merged Response (`GET /api/Languages/german/words/Bank?translated=zh`)
```json
{
  "id": "Bank",
  "$metadata": {
    "translated": "zh",
    "version": "20261107000000000000"
  },
  "lexemes": [
    {
      "id": "Bank#1",
      "type": "noun",
      "gender": "feminine",
      "pronunciation": "baŋk",
      "key_form": "¨-e",
      "entries": [
        {
          "id": "Bank#1.1",
          "meaning": "长椅，公园长椅",
          "usage_note": "供多人坐的长凳",
          "translation_source": "gemini-4-argon",
          "examples": [
            {
              "text": "Wir sitzen auf einer Bank im Park.",
              "translation": "我们坐在公园的长椅上。",
              "translation_source": "gemini-4-argon",
              "source_id": "goethe:die_Bank_1#ex0"
            }
          ]
        }
      ]
    }
  ]
}
```
