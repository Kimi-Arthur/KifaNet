using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Kifa.Api.Files;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise.Api;
using Kifa.Service;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Kifa.Memrise;

public class MemriseClient : IDisposable {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    string lineBreak = new(' ', 100);

    public static string WebDriverUrl { get; set; }
    public static string Cookies { get; set; }
    public static string CsrfToken { get; set; }

    public MemriseCourse Course { get; set; }

    static IWebDriver webDriver;

    static IWebDriver WebDriver {
        get {
            if (webDriver == null) {
                var options = new ChromeOptions();
                //options.AddArgument("--headless");
                webDriver = new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
                    TimeSpan.FromMinutes(10));

                webDriver.Url = "https://app.memrise.com/";

                foreach (var cookie in Cookies.Split("; ")) {
                    var cookiePair = cookie.Split("=", 2);
                    webDriver.Manage().Cookies.AddCookie(new Cookie(cookiePair[0], cookiePair[1],
                        "app.memrise.com", "/", DateTime.Now + TimeSpan.FromDays(365)));
                }
            }

            return webDriver;
        }
    }

    HttpClient? httpClient;

    HttpClient HttpClient {
        get {
            if (httpClient == null) {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("cookie", Cookies);
                httpClient.DefaultRequestHeaders.Add("x-csrftoken", CsrfToken);
                httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
                httpClient.DefaultRequestHeaders.Add("referer", Course.BaseUrl);
            }

            return httpClient;
        }
    }

    GoetheGermanWordRestServiceClient GoetheClient = new();

    GermanWordRestServiceClient WordClient = new();

    MemriseCourseServiceClient CourseClient => MemriseCourse.Client;

    public KifaActionResult AddWordList(GoetheWordList wordList) {
        AddWordsToLevel(Course.Levels[wordList.Id], AddWords(ExpandWords(wordList.Words)).ToList());

        return KifaActionResult.Success;
    }

    public IEnumerable<GoetheGermanWord> ExpandWords(IEnumerable<string> words) {
        foreach (var word in words) {
            var expandedWords = new Queue<GoetheGermanWord>();
            expandedWords.Enqueue(GoetheClient.Get(word));
            while (expandedWords.Count > 0) {
                var goetheWord = expandedWords.Dequeue();
                yield return goetheWord;

                if (goetheWord.Feminine != null) {
                    var feminineWord = goetheWord.Feminine;
                    feminineWord.Meaning = $"(female) {goetheWord.Meaning}";
                    expandedWords.Enqueue(feminineWord);
                }

                if (goetheWord.Abbreviation != null) {
                    var abbrWord = goetheWord.Abbreviation;
                    abbrWord.Meaning = $"{goetheWord.Meaning} (abbr)";
                    expandedWords.Enqueue(abbrWord);
                }
            }
        }
    }

    IEnumerable<string> AddWords(IEnumerable<GoetheGermanWord> words) {
        foreach (var word in words) {
            if (Course.Words.ContainsKey(word.Id)) {
                yield return Course.Words[word.Id].Id;
            }

            var addedWord = AddWord(word);
            Logger.LogResult(addedWord, $"Upload word {word}");
            if (addedWord.Status == KifaActionStatus.OK) {
                var added = addedWord.Response!;
                CourseClient.AddWord(Course.Id, added);
                yield return added.Id;
            }
        }
    }

    void AddWordsToLevel(string levelId, List<string> wordIds) {
        var rendered = new GetLevelRpc {
            HttpClient = HttpClient
        }.Invoke(WebDriver.Url, levelId).Rendered;
        var thingIdReg = new Regex(@"data-thing-id=""(\d+)""");
        var existingThingIds =
            thingIdReg.Matches(rendered).Select(m => m.Groups[1].Value).ToHashSet();

        foreach (var wordId in wordIds.Except(existingThingIds)) {
            Logger.Debug(
                $"Add word {wordId} to level {levelId}: {new AddWordToLevelRpc { HttpClient = HttpClient }.Invoke(WebDriver.Url, levelId, wordId)?.Success}");
        }

        foreach (var wordId in existingThingIds.Except(wordIds)) {
            Logger.Debug(
                $"Remove word {wordId} from level {levelId}: {new RemoveWordFromLevelRpc { HttpClient = HttpClient }.Invoke(WebDriver.Url, levelId, wordId)?.Success}");
        }

        Logger.Debug(
            $"Reorder words for {levelId}: {new ReorderWordsInLevelRpc { HttpClient = HttpClient }.Invoke(WebDriver.Url, levelId, wordIds)?.Success}");
    }

    public KifaActionResult<MemriseWord> AddWord(GoetheGermanWord word,
        bool alwaysCheckAudio = false) {
        var rootWord = WordClient.Get(word.RootWord);
        if (rootWord == null) {
            return new KifaActionResult<MemriseWord> {
                Message = "Failed to get root word.",
                Status = KifaActionStatus.Error
            };
        }

        Logger.Info($"{word.Id} => {rootWord.Id}");

        WebDriver.Url = Course.DatabaseUrl;

        Logger.Debug($"Adding word in {WebDriver.Url}:\n{word}\n{rootWord}");

        var newData = GetDataFromWord(word, rootWord);

        var existingRow = Course.Words.GetValueOrDefault(word.Id)?.Data;

        if (existingRow == null) {
            var thingId = Retry.Run(() => FillBasicWord(newData), (ex, index) => {
                if (index > 5) {
                    throw ex;
                }

                Logger.Warn($"Failed to fill basic row. Retrying ({index + 1}).");
            }, (result, index) => {
                if (result != null) {
                    return true;
                }

                if (index > 5) {
                    throw new Exception("Failed to fill basic row.");
                }

                Logger.Warn($"Retry result ({result}) is not valid. Retrying ({index + 1}).");
                return false;
            });

            Thread.Sleep(TimeSpan.FromSeconds(5));
            existingRow = GetExistingRow(word);
            if (existingRow == null) {
                Logger.Error($"Failed to get filled row of id {thingId}.");
                return new KifaActionResult<MemriseWord>(KifaActionStatus.Error,
                    $"failed to add word {word.Id}");
            }

            if (existingRow.Id != thingId) {
                Logger.Error($"Thing ids ({existingRow.Id}) and ({thingId}) don't match.");
            }
        }

        FillRow(existingRow, newData);

        if (rootWord.PronunciationAudioLinks == null) {
            return new KifaActionResult<MemriseWord>(existingRow);
        }

        var audios = rootWord.PronunciationAudioLinks.Values.SelectMany(links => links).Take(3)
            .ToList();

        if (alwaysCheckAudio || (existingRow.Audios?.Count ?? 0) < audios.Count) {
            UploadAudios(existingRow, rootWord);
        }

        return new KifaActionResult<MemriseWord>(GetExistingRow(word)!);
    }

    void UploadAudios(MemriseWord originalWord, GermanWord baseWord) {
        foreach (var link in baseWord.PronunciationAudioLinks.Where(item => item.Value != null)
                     .OrderBy(item => item.Key).SelectMany(item => item.Value).Take(3)) {
            var newAudioFile = new KifaFile(link);
            newAudioFile.Add(false);
            var info = newAudioFile.FileInfo!;
            if (originalWord.Audios.Any(audio
                    => audio.Size == info.Size && audio.Md5 == info.Md5)) {
                Logger.Debug($"{link} for {baseWord.Id} ({originalWord.Id}) already exists.");
                continue;
            }

            Logger.Debug($"Uploading {link} for {baseWord.Id} ({originalWord.Id}).");
            new UploadAudioRpc {
                HttpClient = HttpClient
            }.Invoke(WebDriver.Url, originalWord.Id, Course.Columns["Audios"], CsrfToken,
                newAudioFile.ReadAsBytes());
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }

    MemriseWord? GetExistingRow(GoetheGermanWord word)
        => GetExistingRow(word, TrimBracket(word.Id)) ?? (word.Meaning != null
            ? GetExistingRow(word, TrimBracket(word.Meaning))
            : null);

    MemriseWord? GetExistingRow(GoetheGermanWord word, string searchQuery) {
        var searchBar = WebDriver.FindElement(By.CssSelector("input#search_string"));
        searchBar.Clear();
        searchBar.SendKeys(searchQuery);
        searchBar.Submit();

        return Course.GetWordsInPage().FirstOrDefault(w => SameWord(w, word));
    }

    bool SameWord(MemriseWord? memriseWord, GoetheGermanWord goetheGermanWord)
        => memriseWord != null && memriseWord.Data[Course.Columns["German"]] == goetheGermanWord.Id;

    static string TrimBracket(string content) {
        var reg = new Regex(@"^(\(.*\) )?(.*)( \(.*\))?$");
        return reg.Match(content).Groups[2].Value;
    }

    string? FillBasicWord(Dictionary<string, string> newData)
        => new AddWordRpc {
            HttpClient = HttpClient
        }.Invoke(Course.DatabaseId, Course.BaseUrl, newData)?.Thing.Id.ToString();

    int FillRow(MemriseWord originalData, Dictionary<string, string> newData) {
        var updatedFields = 0;
        foreach (var (dataKey, newValue) in newData) {
            if (originalData.Data.GetValueOrDefault(dataKey) != newValue) {
                new UpdateWordRpc {
                    HttpClient = HttpClient
                }.Invoke(WebDriver.Url, originalData.Id, dataKey, newValue);
                updatedFields++;
            }
        }

        return updatedFields;
    }

    Dictionary<string, string> GetDataFromWord(GoetheGermanWord word, GermanWord? baseWord) {
        var data = new Dictionary<string, string> {
            { Course.Columns["German"], word.Id },
            { Course.Columns["English"], word.Meaning }
        };

        data[Course.Columns["Form"]] = word.Form ?? "";

        data[Course.Columns["Pronunciation"]] =
            baseWord?.Pronunciation != null ? $"[{baseWord.Pronunciation}]" : "";

        data[Course.Columns["Examples"]] =
            word.Examples?.Count > 0 && !word.Examples[0].StartsWith("example")
                ? string.Join(lineBreak,
                    word.Examples.Select((example, index) => $"{index + 1}. {example}"))
                : "";

        data[Course.Columns["Etymology"]] = baseWord?.Etymology != null
            ? string.Join(lineBreak,
                baseWord.Etymology.Select(segment
                    => segment + ": " + (GoetheClient.Get(segment)?.Meaning ??
                                         WordClient.Get(segment)?.Meaning ?? "<unknown>")))
            : "";

        return data;
    }

    public void Dispose() {
        webDriver?.Dispose();
        httpClient?.Dispose();
    }
}
