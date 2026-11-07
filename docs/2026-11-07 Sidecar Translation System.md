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
  - Contains a **sparse `TDataModel` tree** with only the translated fields populated, plus `$metadata.translated`.

### 2.2 `Translated` in `DataMetadata` and `KifaDataOptions`
To avoid ambiguity with an item's source/subject language, both the query parameter and `$metadata` field are named **`translated`** (typed as [`Kifa.Language?`](../src/Kifa/Cultures/Language.cs)):

1. **`DataMetadata`** ([`DataMetadata.cs`](../src/Kifa.Service/DataModel/DataMetadata.cs)):
   ```csharp
   public Language? Translated { get; set; }
   ```
   - Omitted (`null`) in the base file (`Bank.json`).
   - Present (`"$metadata": { "translated": "zh" }`) in `Bank.$zh.json` and in the merged response returned when `?translated=zh` is requested.

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

## 3. Recursive Tree Merging (`MergeTranslationTree`)

When `GET /api/{ModelId}/{id}?translated=zh` is called:
1. Read base `TDataModel` from `{id}.json`.
2. Read sparse translation overlay from `{id}.$zh.json` (if it exists).
3. Merge the sparse `JObject` tree onto the base `JObject` tree using recursive rules:
   - **Scalar / Object Properties**: Non-null properties in the translation tree overwrite the corresponding properties on the base object (e.g., `meaning`, `usage_note`, `translation`, `translation_source`).
   - **Arrays (`JArray`)**:
     - **ID-Keyed Elements** (array items are `JObject`s with an `"id"` field, such as `lexemes` `"Bank#1"` or `entries` `"Bank#1.1"`): Match overlay elements to base elements by `"id"` and recursively merge. Unmatched base elements remain untouched.
     - **Positional Elements** (array items without `"id"`, such as `examples[i]` or `phrases[i]`): Match by index `i` and recursively merge.
   - **Metadata**: Set `$metadata.translated = "zh"` on the merged result while preserving the base record's content `$metadata.version`.

---

## 4. Concrete Example

### Base File (`Bank.json`)
```json
{
  "id": "Bank",
  "$metadata": {
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
  "id": "Bank",
  "$metadata": {
    "translated": "zh",
    "version": "20261107000500000000"
  },
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
