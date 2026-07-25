using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using AngleSharp.Dom;
using NLog;

namespace Kifa.Subtitle.Subcat;

public static class SubcatClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient HttpClient = new();

    public const string UrlPrefix = "https://www.subtitlecat.com";

    public static Dictionary<Language, (string Link, bool NeedsGeneration)> GetDownloadLinks(
        IDocument doc, List<Language> languages) {
        var links = new Dictionary<Language, (string Link, bool NeedsGeneration)>();
        foreach (var lang in languages) {
            var link = GetDownloadLink(doc, lang);
            if (link != null) {
                links[lang] = link.Value;
            }
        }

        return links;
    }

    public static (string Link, bool NeedsGeneration)?
        GetDownloadLink(IDocument doc, Language lang) {
        var subcatLang = GetSubcatLanguage(lang);
        var element = doc.GetElementById($"download_{subcatLang}");
        if (element != null) {
            var href = element.Attributes["href"]?.Value;
            if (!string.IsNullOrEmpty(href)) {
                return ($"{UrlPrefix}/{href.TrimStart('/')}", false);
            }
        }

        var translateElement = doc.GetElementById(subcatLang);
        if (translateElement != null) {
            var onClick = translateElement.Attributes["onclick"]?.Value;
            if (!string.IsNullOrEmpty(onClick)) {
                var match = Regex.Match(onClick,
                    @"translate_from_server_folder\('([^']+)',\s*'([^']+)',\s*'([^']+)'\)");
                if (match.Success) {
                    var origFile = match.Groups[2].Value;
                    var folder = match.Groups[3].Value;
                    return ($"{UrlPrefix}/{folder.Trim('/')}/{origFile}", true);
                }
            }
        }

        return null;
    }

    public static string DownloadOrGenerate((string Link, bool NeedsGeneration) choice,
        Language language) {
        if (choice.NeedsGeneration) {
            Logger.Debug($"Will generate subtitle for {language.Code} from {choice.Link}");
            var subcatLang = GetSubcatLanguage(language);
            var origContent = HttpClient.SendWithRetry(choice.Link).GetString();
            var (content, detectedSourceLang) = TranslateSrt(origContent, subcatLang);

            var origFileName = choice.Link.Split('/').Last();
            var savingFileName = origFileName.Replace("-orig.srt", ".srt");
            var serverUrl =
                UploadTranslation(savingFileName, content, subcatLang, detectedSourceLang);

            if (!string.IsNullOrEmpty(serverUrl)) {
                try {
                    var fullUrl = serverUrl.StartsWith("http")
                        ? serverUrl
                        : $"{UrlPrefix}/{serverUrl.TrimStart('/')}";
                    Logger.Debug($"Fetching uploaded translation from server: {fullUrl}");
                    return HttpClient.SendWithRetry(fullUrl).GetString();
                } catch (Exception ex) {
                    Logger.Warn(ex,
                        "Failed to download uploaded translation from server, falling back to local generated content.");
                }
            }

            return content;
        }

        Logger.Debug($"Will download subtitle for {language.Code} from {choice.Link}");
        return HttpClient.SendWithRetry(choice.Link).GetString();
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

    public static string GetSubtitlePath(string videoParentPath, string subcatLink,
        Language language) {
        var (subcatId, title) = ParseSubcatUrl(subcatLink);
        var sourcesPath = GetSourcesPath(videoParentPath);
        return
            $"{sourcesPath}/{title}.{string.FormatOrEmpty($"{subcatId}.")}subcat.{language.Code}.srt";
    }
}
