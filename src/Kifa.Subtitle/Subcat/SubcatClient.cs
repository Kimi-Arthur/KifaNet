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
using Kifa.Subtitle.Srt;
using NLog;

namespace Kifa.Subtitle.Subcat;

public static class SubcatClient {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static readonly HttpClient HttpClient = new();

    public const string UrlPrefix = "https://www.subtitlecat.com";

    static readonly Regex TranslatedFromRegex = new(@"\(translated from ([^)]+)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex SizeRegex = new(@"SIZE\s*(\d+(?:\.\d+)?\s*[KMG]?B)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex DownloadsRegex =
        new(@"(\d+)\s+downloads?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Regex LanguagesRegex =
        new(@"(\d+)\s+languages?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static List<SubcatChoice> FindSubtitles(string keyword, List<Language> languages) {
        var doc = HttpClient.SendWithRetry($"{UrlPrefix}/index.php?search={keyword}").GetString()
            .GetDocument();
        return ParseSearchResults(doc).SelectMany(sub => {
            var pageLink = GetFullUrl(sub.Link);
            var pageContent = HttpClient.SendWithRetry(pageLink).GetString().GetDocument();
            var downloadUrls = GetDownloadUrls(pageContent, languages);
            return downloadUrls.Select(kv => new SubcatChoice {
                OriginalLink = GetFullUrl(sub.OriginalLink),
                DownloadLink = kv.Value != null ? GetFullUrl(kv.Value) : null,
                Language = kv.Key,
                SourceLanguage = sub.SourceLanguage,
                Size = sub.Size,
                DownloadCount = sub.DownloadCount,
                LanguageCount = sub.LanguageCount
            });
        }).ToList();
    }

    public static List<(string Link, string OriginalLink, string? SourceLanguage, string? Size, int
        DownloadCount, int LanguageCount)> ParseSearchResults(IDocument doc) {
        var table = doc.GetElementsByClassName("sub-table").FirstOrDefault();
        return table?.QuerySelectorAll("tr").SelectMany(ExtractSubtitle).Take(10).ToList() ?? [];
    }

    static IEnumerable<(string Link, string OriginalLink, string? SourceLanguage, string? Size, int
        DownloadCount, int LanguageCount)> ExtractSubtitle(IElement row) {
        var anchor = row.QuerySelector("a");
        if (anchor == null) {
            yield break;
        }

        var href = anchor.Attributes["href"]?.Value;
        if (string.IsNullOrEmpty(href)) {
            yield break;
        }

        var lastDot = href.LastIndexOf('.');
        var originalLink = (lastDot >= 0 ? href[..lastDot] : href) + "-orig.srt";

        var rowText = row.TextContent;

        string? sourceLanguage = null;
        var translatedMatch = TranslatedFromRegex.Match(rowText);
        if (translatedMatch.Success) {
            sourceLanguage = translatedMatch.Groups[1].Value.Trim();
        }

        var size = row.QuerySelector(".sub-table__metric-value")?.TextContent?.Trim();
        if (string.IsNullOrEmpty(size)) {
            var sizeMatch = SizeRegex.Match(rowText);
            if (sizeMatch.Success) {
                size = sizeMatch.Groups[1].Value.Trim();
            }
        }

        var downloads = 0;
        var downloadsMatch = DownloadsRegex.Match(rowText);
        if (downloadsMatch.Success) {
            downloads = int.Parse(downloadsMatch.Groups[1].Value);
        }

        var languagesCount = 0;
        var languagesMatch = LanguagesRegex.Match(rowText);
        if (languagesMatch.Success) {
            languagesCount = int.Parse(languagesMatch.Groups[1].Value);
        }

        yield return (Link: href, OriginalLink: originalLink, SourceLanguage: sourceLanguage,
            Size: size, DownloadCount: downloads, LanguageCount: languagesCount);
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

    public static (string Content, string Filename) DownloadOrGenerate(SubcatChoice choice) {
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

            var subcatLink = serverUrl ?? choice.OriginalLink;
            var filename = GetSubtitleFileName(subcatLink, choice.Language);

            if (serverUrl != null) {
                var fullUrl = GetFullUrl(serverUrl);
                try {
                    Logger.Debug($"Fetching uploaded translation from server: {fullUrl}");
                    var downloadedContent = HttpClient.SendWithRetry(fullUrl).GetString();
                    return (downloadedContent, filename);
                } catch (Exception ex) {
                    Logger.Warn(ex,
                        "Failed to download uploaded translation from server, falling back to local generated content.");
                    return (content, filename);
                }
            }

            return (content, filename);
        }

        Logger.Debug(
            $"Will download subtitle for {choice.Language.Code} from {choice.DownloadLink}");
        var downloadLink = choice.DownloadLink.Checked();
        var downloadContent = HttpClient.SendWithRetry(downloadLink).GetString();
        var downloadFilename = GetSubtitleFileName(downloadLink, choice.Language);
        return (downloadContent, downloadFilename);
    }

    public static string? UploadTranslation(string filename, string content, string language,
        string originalLanguage = "auto") {
        try {
            var response =
                HttpClient.Call(new SubcatUploadRpc(filename, content, language, originalLanguage));
            Logger.Debug(
                $"Uploaded translation to SubtitleCat response: echo={response?.Echo}, url={response?.Url}");
            return response?.Url;
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
        var srtDoc = SrtDocument.Parse(srtContent);
        var detectedSourceLang = "auto";

        var batches = new List<List<int>>();
        var currentBatch = new List<int>();
        var currentBatchLength = 0;

        for (var i = 0; i < srtDoc.Lines.Count; i++) {
            var lineText = NormalizeSrtLine(srtDoc.Lines[i].Text.Content);

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
            var batchText = string.Join("\n",
                batch.Select(idx => NormalizeSrtLine(srtDoc.Lines[idx].Text.Content)));

            try {
                var url =
                    $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLang}&dt=t&q={Uri.EscapeDataString(batchText)}";
                var responseJson = HttpClient.SendWithRetry(url).GetString();
                using var jsonDoc = JsonDocument.Parse(responseJson);
                var root = jsonDoc.RootElement;
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
                        srtDoc.Lines[batch[j]].Text.Content = translatedLinesInBatch[j];
                    }
                } else {
                    foreach (var lineIdx in batch) {
                        var singleText = NormalizeSrtLine(srtDoc.Lines[lineIdx].Text.Content);
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

                            srtDoc.Lines[lineIdx].Text.Content = sSb.ToString();
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Warn(ex, "Failed to translate batch, keeping original lines.");
            }
        }

        return (srtDoc.ToString(), detectedSourceLang);
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

    public static string GetSubtitleFileName(string subcatLink, Language language) {
        var (subcatId, title) = ParseSubcatUrl(subcatLink);
        var idSegment = string.FormatOrEmpty($"{subcatId}.");
        return $"{title}.{idSegment}subcat.{language.Code}.srt";
    }

    public static string GetSubtitlePath(string videoParentPath, SubcatChoice choice)
        => GetSubtitlePath(videoParentPath, choice.DownloadLink ?? choice.OriginalLink,
            choice.Language);

    public static string GetSubtitlePath(string videoParentPath, string subcatLink,
        Language language)
        => $"{GetSourcesPath(videoParentPath)}/{GetSubtitleFileName(subcatLink, language)}";
}
