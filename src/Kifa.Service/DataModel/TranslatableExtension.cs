using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kifa.Service;

public static class TranslatableExtension {
    static readonly JsonSerializer DefaultSerializer =
        JsonSerializer.Create(KifaJsonSerializerSettings.Default);

    static readonly JsonSerializer MergeSerializer =
        JsonSerializer.Create(KifaJsonSerializerSettings.Merge);

    public static bool TryParseTranslationId(this string? id,
        [NotNullWhen(true)] out string? baseId,
        [NotNullWhen(true)] out Language? language) {
        baseId = null;
        language = null;
        if (string.IsNullOrEmpty(id)) {
            return false;
        }

        var lastSlash = id.LastIndexOf('/');
        var markerIndex = id.LastIndexOf(".$", StringComparison.Ordinal);
        if (markerIndex <= lastSlash + 1 || markerIndex + 2 >= id.Length) {
            return false;
        }

        baseId = id[..markerIndex];
        language = id[(markerIndex + 2)..];
        return true;
    }

    public static string GetTranslationId(this string baseId, Language language)
        => $"{baseId}.${language.Code}";

    public static TDataModel MergeTranslation<TDataModel>(this TDataModel baseData,
        TDataModel translation)
        where TDataModel : DataModel {
        var baseObj = JObject.FromObject(baseData, DefaultSerializer);
        var overlayObj = JObject.FromObject(translation, MergeSerializer);

        MergeJsonObject(baseObj, overlayObj, isRoot: true);

        var result = baseObj.ToObject<TDataModel>(DefaultSerializer).Checked();
        if (translation.Id.TryParseTranslationId(out _, out var lang) &&
            lang != Language.Unknown) {
            result.Metadata ??= new DataMetadata();
            result.Metadata.Translated = lang;
        }

        return result;
    }

    static void MergeJsonObject(JObject target, JObject overlay, bool isRoot = false) {
        foreach (var prop in overlay.Properties()) {
            if (isRoot && (prop.Name == "$metadata" || prop.Name == "id")) {
                continue;
            }

            var targetValue = target[prop.Name];
            var overlayValue = prop.Value;

            if (targetValue is JObject targetChildObj && overlayValue is JObject overlayChildObj) {
                MergeJsonObject(targetChildObj, overlayChildObj);
            } else if (targetValue is JArray targetArray && overlayValue is JArray overlayArray) {
                MergeJsonArray(targetArray, overlayArray);
            } else {
                target[prop.Name] = overlayValue.DeepClone();
            }
        }
    }

    static void MergeJsonArray(JArray targetArray, JArray overlayArray) {
        if (overlayArray.Count == 0) {
            return;
        }

        // If elements are primitives (e.g. List<string>), replace the array contents.
        if (overlayArray.FirstOrDefault(item => item.Type != JTokenType.Null) is not JObject) {
            targetArray.ReplaceAll(overlayArray.Select(item => item.DeepClone()));
            return;
        }

        for (var i = 0; i < overlayArray.Count; i++) {
            // Skip null placeholders in positional object arrays.
            if (overlayArray[i] is not JObject overlayItemObj) {
                continue;
            }

            var itemId = overlayItemObj["id"]?.Value<string>();
            if (!string.IsNullOrEmpty(itemId)) {
                var matchingTarget = targetArray.OfType<JObject>()
                    .FirstOrDefault(t => t["id"]?.Value<string>() == itemId);
                if (matchingTarget != null) {
                    MergeJsonObject(matchingTarget, overlayItemObj);
                }
            } else if (i < targetArray.Count && targetArray[i] is JObject targetItemObj) {
                MergeJsonObject(targetItemObj, overlayItemObj);
            }
        }
    }
}
