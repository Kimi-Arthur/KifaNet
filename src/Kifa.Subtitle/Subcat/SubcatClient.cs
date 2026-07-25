using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using AngleSharp.Dom;
using Kifa.Html;
using NLog;

namespace Kifa.Subtitle.Subcat;

public static class SubcatClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient HttpClient = new();

    public const string UrlPrefix = "https://www.subtitlecat.com";

    public static List<SubcatChoice> FindSubtitles(string keyword, List<Language> languages) {
        var doc = HttpClient.SendWithRetry($"{UrlPrefix}/index.php?search={keyword}").GetString()
            .GetDocument();
        var table = doc.GetElementsByClassName("sub-table").FirstOrDefault();
        var elements = table?.QuerySelectorAll("a");

        if (elements == null || !elements.Any()) {
            return [];
        }

        var rawSubtitles = elements.Take(10).Select(element => {
            var href = element.Attributes["href"].Checked().Value;
            var (id, title) = ParseSubcatUrl(href);
            return (Id: id, Title: title, Link: href);
        }).ToList();

        return rawSubtitles.SelectMany(sub => {
            var pageLink = $"{UrlPrefix}/{sub.Link.TrimStart('/')}";
            var pageContent = HttpClient.SendWithRetry(pageLink).GetString().GetDocument();
            var downloadLinks = GetDownloadLinks(pageContent, languages);
            return downloadLinks.Select(kv => new SubcatChoice {
                Id = sub.Id,
                Title = sub.Title,
                Language = kv.Key,
                NeedsGeneration = kv.Value
            });
        }).ToList();
    }

    public static Dictionary<Language, bool> GetDownloadLinks(IDocument doc,
        List<Language> languages) {
        var links = new Dictionary<Language, bool>();
        foreach (var lang in languages) {
            var needsGeneration = GetDownloadLink(doc, lang);
            if (needsGeneration != null) {
                links[lang] = needsGeneration.Value;
            }
        }

        return links;
    }

    public static bool? GetDownloadLink(IDocument doc, Language lang) {
        var subcatLang = GetSubcatLanguage(lang);
        if (doc.GetElementById($"download_{subcatLang}") != null) {
            return false;
        }

        if (doc.GetElementById(subcatLang) != null) {
            return true;
        }

        return null;
    }

    public static string DownloadOrGenerate(SubcatChoice choice) {
        if (choice.NeedsGeneration) {
            Logger.Debug(
                $"Will generate subtitle for {choice.Language.Code} from {choice.GenerateLink}");
            var subcatLang = GetSubcatLanguage(choice.Language);
            var origContent = HttpClient.SendWithRetry(choice.GenerateLink).GetString();
            var (content, detectedSourceLang) = TranslateSrt(origContent, subcatLang);

            var origFileName = choice.GenerateLink.Split('/').Last();
            var savingFileName = origFileName.Replace("-orig.srt", ".srt");
            var serverUrl =
                UploadTranslation(savingFileName, content, subcatLang, detectedSourceLang);

            if (!string.IsNullOrEmpty(serverUrl)) {
                try {
                    var fullUrl = serverUrl.StartsWith("http")
                        ? serverUrl
                        : $"{UrlPrefix}/{serverUrl.TrimStart('/')}";
                    if (fullUrl != choice.DownloadLink) {
                        Logger.Warn(
                            $"Uploaded translation server URL '{fullUrl}' does not match expected download link '{choice.DownloadLink}'.");
                    }

                    Logger.Debug($"Fetching uploaded translation from server: {fullUrl}");
                    return HttpClient.SendWithRetry(fullUrl).GetString();
                } catch (Exception ex) {
                    Logger.Warn(ex,
                        "Failed to download uploaded translation from server, falling back to local generated content.");
                }
            }

            return content;
        }

        Logger.Debug(
            $"Will download subtitle for {choice.Language.Code} from {choice.DownloadLink}");
        return HttpClient.SendWithRetry(choice.DownloadLink).GetString();
    }

    public static string? UploadTranslation(string filename, string content, string language,
        string origLanguage = "auto") {
        try {
            var contentParams = new Dictionary<string, string> {
                { "filename", filename },
                { "content", content },
                { "language", language },
                { "orig_language", origLanguage }
            };
            var response = HttpClient.SendWithRetry(()
                => new HttpRequestMessage(HttpMethod.Post, $"{UrlPrefix}/upload_subtitles.php") {
                    Content = new FormUrlEncodedContent(contentParams)
                }).GetString();
            Logger.Debug($"Uploaded translation to SubtitleCat response: {response}");

            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("url", out var urlElement)) {
                return urlElement.GetString();
            }
        } catch (Exception ex) {
            Logger.Warn(ex, "Failed to upload generated translation to SubtitleCat.");
        }

        return null;
    }

    public static (string Content, string DetectedSourceLanguage) TranslateSrt(string srtContent,
        string targetLang) {
        var lines = srtContent.Replace("\r\n", "\n").Split('\n');
        var translatedLines = (string[]) lines.Clone();
        var detectedSourceLang = "auto";

        var batches = new List<List<int>>();
        var currentBatch = new List<int>();
        var currentBatchLength = 0;

        for (var i = 0; i < lines.Length; i++) {
            if (IsTimecodeOrIndex(lines[i])) {
                continue;
            }

            var lineText = Regex.Replace(lines[i], @"<font[^>]*>", "", RegexOptions.IgnoreCase);
            lineText = Regex.Replace(lineText, @"</font>", "", RegexOptions.IgnoreCase);
            lineText = lineText.Replace("&", "and");

            if (currentBatchLength + lineText.Length + 1 > 500 && currentBatch.Count > 0) {
                batches.Add(currentBatch);
                currentBatch = new List<int>();
                currentBatchLength = 0;
            }

            currentBatch.Add(i);
            currentBatchLength += lineText.Length + 1;
        }

        if (currentBatch.Count > 0) {
            batches.Add(currentBatch);
        }

        foreach (var batch in batches) {
            var batchText = string.Join("\n", batch.Select(idx => {
                var lineText =
                    Regex.Replace(lines[idx], @"<font[^>]*>", "", RegexOptions.IgnoreCase);
                lineText = Regex.Replace(lineText, @"</font>", "", RegexOptions.IgnoreCase);
                return lineText.Replace("&", "and");
            }));

            try {
                var url =
                    $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(batchText)}";
                var responseJson = HttpClient.SendWithRetry(url).GetString();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 2 &&
                    root[2].ValueKind == JsonValueKind.String) {
                    var srcLang = root[2].GetString();
                    if (!string.IsNullOrEmpty(srcLang)) {
                        detectedSourceLang = srcLang;
                    }
                }

                var sb = new StringBuilder();
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0) {
                    var sentences = root[0];
                    if (sentences.ValueKind == JsonValueKind.Array) {
                        foreach (var sentence in sentences.EnumerateArray()) {
                            if (sentence.ValueKind == JsonValueKind.Array &&
                                sentence.GetArrayLength() > 0) {
                                sb.Append(sentence[0].GetString());
                            }
                        }
                    }
                }

                var translatedLinesInBatch = sb.ToString().Split('\n');
                if (translatedLinesInBatch.Length == batch.Count) {
                    for (var j = 0; j < batch.Count; j++) {
                        translatedLines[batch[j]] = translatedLinesInBatch[j];
                    }
                } else {
                    foreach (var lineIdx in batch) {
                        var singleText = Regex.Replace(lines[lineIdx], @"<font[^>]*>", "",
                            RegexOptions.IgnoreCase);
                        singleText = Regex
                            .Replace(singleText, @"</font>", "", RegexOptions.IgnoreCase)
                            .Replace("&", "and");
                        var singleUrl =
                            $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(singleText)}";
                        var singleResponse = HttpClient.SendWithRetry(singleUrl).GetString();
                        using var singleDoc = JsonDocument.Parse(singleResponse);
                        if (singleDoc.RootElement.ValueKind == JsonValueKind.Array &&
                            singleDoc.RootElement.GetArrayLength() > 0) {
                            var sSb = new StringBuilder();
                            foreach (var sentence in singleDoc.RootElement[0].EnumerateArray()) {
                                if (sentence.ValueKind == JsonValueKind.Array &&
                                    sentence.GetArrayLength() > 0) {
                                    sSb.Append(sentence[0].GetString());
                                }
                            }

                            translatedLines[lineIdx] = sSb.ToString();
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Warn(ex, "Failed to translate batch, keeping original lines.");
            }
        }

        return (string.Join("\n", translatedLines), detectedSourceLang);
    }

    static bool IsTimecodeOrIndex(string line) {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return true;
        if (Regex.IsMatch(trimmed, @"^\d+$"))
            return true;
        if (Regex.IsMatch(trimmed,
                @"^\d{2}:\d{2}:\d{2}[,\.]\d{3}\s*-->\s*\d{2}:\d{2}:\d{2}[,\.]\d{3}"))
            return true;
        return false;
    }

    public static string GetSubcatLanguage(Language lang)
        => lang.Code == "zh" ? "zh-CN" : lang.Code;

    public static string GetSourcesPath(string videoParentPath) => $"/Sources{videoParentPath}";

    static readonly Regex SubcatUrlRegex =
        new(@"(?:^|/)subs/(\d+)/([^/]+?)(?:-orig)?(?:\.(?:html|srt|ass))?$", RegexOptions.Compiled);

    public static (string? Id, string Title) ParseSubcatUrl(string url) {
        var match = SubcatUrlRegex.Match(url);
        if (match.Success) {
            return (match.Groups[1].Value, match.Groups[2].Value);
        }

        var title = Path.GetFileNameWithoutExtension(url);
        title = Regex.Replace(title, @"-orig$", "");
        return (null, title.Trim());
    }

    public static string GetSubtitlePath(string videoParentPath, SubcatChoice choice) {
        var idSegment = string.FormatOrEmpty($"{choice.Id}.");
        var sourcesPath = GetSourcesPath(videoParentPath);
        return $"{sourcesPath}/{choice.Title}.{idSegment}subcat.{choice.Language.Code}.srt";
    }

    public static string GetSubtitlePath(string videoParentPath, string subcatLink,
        Language language) {
        var (subcatId, title) = ParseSubcatUrl(subcatLink);
        var sourcesPath = GetSourcesPath(videoParentPath);
        return
            $"{sourcesPath}/{title}.{string.FormatOrEmpty($"{subcatId}.")}subcat.{language.Code}.srt";
    }
}
