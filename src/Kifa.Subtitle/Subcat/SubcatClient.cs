using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            var originalLink = Path.ChangeExtension(href, "-orig.srt");
            return (Link: href, OriginalLink: originalLink);
        }).ToList();

        return rawSubtitles.SelectMany(sub => {
            var pageLink = GetFullUrl(sub.Link);
            var pageContent = HttpClient.SendWithRetry(pageLink).GetString().GetDocument();
            var downloadUrls = GetDownloadUrls(pageContent, languages);
            return downloadUrls.Select(kv => new SubcatChoice {
                OriginalLink = GetFullUrl(sub.OriginalLink),
                DownloadLink = kv.Value != null ? GetFullUrl(kv.Value) : null,
                Language = kv.Key
            });
        }).ToList();
    }

    // Parses a single subtitle page HTML document and retrieves download URLs (or null if generation is needed) for target languages.
    public static Dictionary<Language, string?>
        GetDownloadUrls(IDocument doc, List<Language> languages)
        => languages.Select(lang => (Language: lang, Url: GetDownloadUrl(doc, lang)))
            .Where(x => x.Url != null).ToDictionary(x => x.Language,
                x => x.Url == "" ? null : x.Url);

    // Returns relative download URL string if direct link exists, "" if translation button exists, or null if absent.
    public static string? GetDownloadUrl(IDocument doc, Language lang) {
        var subcatLang = GetSubcatLanguage(lang);
        var downloadElement = doc.GetElementById($"download_{subcatLang}");
        if (downloadElement != null) {
            return downloadElement.Attributes["href"]?.Value ?? "";
        }

        if (doc.GetElementById(subcatLang) != null) {
            return "";
        }

        return null;
    }

    public static string GetFullUrl(string link) {
        var fullUrl = link.StartsWith("http") ? link : $"{UrlPrefix}/{link.TrimStart('/')}";
        return new Uri(fullUrl).AbsoluteUri;
    }

    public static string DownloadOrGenerate(SubcatChoice choice) {
        if (choice.NeedsGeneration) {
            Logger.Debug(
                $"Will generate subtitle for {choice.Language.Code} from {choice.OriginalLink}");
            var subcatLang = GetSubcatLanguage(choice.Language);
            var originalContent = HttpClient.SendWithRetry(choice.OriginalLink).GetString();
            var (content, detectedSourceLang) = TranslateSrt(originalContent, subcatLang);

            var originalFileName = choice.OriginalLink.Split('/').Last();
            var savingFileName = originalFileName.Replace("-orig.srt", ".srt");
            var serverUrl =
                UploadTranslation(savingFileName, content, subcatLang, detectedSourceLang);

            if (serverUrl != null) {
                try {
                    var fullUrl = GetFullUrl(serverUrl);
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
        return HttpClient.SendWithRetry(choice.DownloadLink.Checked()).GetString();
    }

    public static string? UploadTranslation(string filename, string content, string language,
        string originalLanguage = "auto") {
        try {
            var contentParams = new Dictionary<string, string> {
                { "filename", filename },
                { "content", content },
                { "language", language },
                { "orig_language", originalLanguage }
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

    static readonly Regex FontTagRegex =
        new(@"</?font[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static string NormalizeSrtLine(string line)
        => FontTagRegex.Replace(line, "").Replace("&", "and");

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

            var lineText = NormalizeSrtLine(lines[i]);

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
            var batchText = string.Join("\n", batch.Select(idx => NormalizeSrtLine(lines[idx])));

            try {
                var url =
                    $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(batchText)}";
                var responseJson = HttpClient.SendWithRetry(url).GetString();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 2 &&
                    root[2].ValueKind == JsonValueKind.String) {
                    var srcLang = root[2].GetString();
                    if (srcLang != null) {
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
                        var singleText = NormalizeSrtLine(lines[lineIdx]);
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

    static readonly Regex IndexRegex = new(@"^\d+$", RegexOptions.Compiled);

    static readonly Regex TimecodeRegex =
        new(@"^\d{2}:\d{2}:\d{2}[,\.]\d{3}\s*-->\s*\d{2}:\d{2}:\d{2}[,\.]\d{3}",
            RegexOptions.Compiled);

    static bool IsTimecodeOrIndex(string line) {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) {
            return true;
        }

        if (IndexRegex.IsMatch(trimmed)) {
            return true;
        }

        if (TimecodeRegex.IsMatch(trimmed)) {
            return true;
        }

        return false;
    }

    public static string GetSubcatLanguage(Language lang)
        => lang.Code == "zh" ? "zh-CN" : lang.Code;

    public static string GetSourcesPath(string videoParentPath) => $"/Sources{videoParentPath}";

    static readonly Regex SubcatUrlRegex =
        new(
            @"(?:^|/)subs/(?:(\d+)/)?([^/]+?)(?:-[a-z]{2}(?:-[A-Z]{2})?)?(?:-orig)?(?:\.(?:html|srt|ass))?$",
            RegexOptions.Compiled);

    static readonly Regex TitleSuffixRegex =
        new(@"-(?:orig|[a-z]{2}(?:-[A-Z]{2})?)$", RegexOptions.Compiled);

    public static (string? Id, string Title) ParseSubcatUrl(string url) {
        var match = SubcatUrlRegex.Match(url);
        if (match.Success) {
            var id = match.Groups[1].Success ? match.Groups[1].Value : null;
            return (id, Unescape(match.Groups[2].Value));
        }

        var title = Path.GetFileNameWithoutExtension(url);
        title = TitleSuffixRegex.Replace(title, "");
        return (null, Unescape(title.Trim()));
    }

    public static string Unescape(string text)
        => string.IsNullOrEmpty(text)
            ? text
            : WebUtility.HtmlDecode(Uri.UnescapeDataString(WebUtility.HtmlDecode(text)));

    public static string GetSubtitlePath(string videoParentPath, SubcatChoice choice)
        => GetSubtitlePath(videoParentPath, choice.DownloadLink ?? choice.OriginalLink,
            choice.Language);

    public static string GetSubtitlePath(string videoParentPath, string subcatLink,
        Language language) {
        var (subcatId, title) = ParseSubcatUrl(subcatLink);
        var idSegment = string.FormatOrEmpty($"{subcatId}.");
        var sourcesPath = GetSourcesPath(videoParentPath);
        return $"{sourcesPath}/{title}.{idSegment}subcat.{language.Code}.srt";
    }
}
