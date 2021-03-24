using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Kifa.Api.Files;
using Kifa.IO;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise.Api;
using Kifa.Service;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Kifa.Memrise {
    public class MemriseClient : IDisposable {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        string lineBreak = new(' ', 100);

        public static string WebDriverUrl { get; set; }
        public static string Cookies { get; set; }
        public static string CsrfToken { get; set; }

        public MemriseCourse Course { get; set; }

        IWebDriver webDriver;

        IWebDriver WebDriver {
            get {
                if (webDriver == null) {
                    var options = new ChromeOptions();
                    //options.AddArgument("--headless");
                    webDriver = new RemoteWebDriver(new Uri(WebDriverUrl), options.ToCapabilities(),
                        TimeSpan.FromMinutes(10));

                    webDriver.Url = "https://app.memrise.com/";

                    foreach (var cookie in Cookies.Split("; ")) {
                        var cookiePair = cookie.Split("=", 2);
                        webDriver.Manage().Cookies.AddCookie(new Cookie(cookiePair[0], cookiePair[1], "app.memrise.com",
                            "/", DateTime.Now + TimeSpan.FromDays(365)));
                    }
                }

                return webDriver;
            }
        }

        HttpClient httpClient;

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

        private GermanWordRestServiceClient WordClient = new();

        public KifaActionResult AddWordList(GoetheWordList wordList) {
            var levelId = Course.Levels[wordList.Id];

            var wordIds = new List<string>();
            foreach (var word in wordList.Words) {
                var goetheWord = GoetheClient.Get(word);
                var rootWord = WordClient.Get(goetheWord.RootWord);
                logger.Info($"{goetheWord.Id} => {rootWord?.Id}");
                var addedWord = AddWord(goetheWord, rootWord);
                logger.LogResult(addedWord, $"Upload word {word}");
                if (addedWord.Status == KifaActionStatus.OK) {
                    wordIds.Add(addedWord.Response);
                }
            }

            AddWordsToLevel(levelId, wordIds);

            return KifaActionResult.SuccessActionResult;
        }

        void AddWordsToLevel(string levelId, List<string> wordIds) {
            var rendered = new GetLevelRpc {HttpClient = HttpClient}.Call(WebDriver.Url, levelId).Rendered;
            var thingIdReg = new Regex(@"data-thing-id=""(\d+)""");
            var existingThingIds = thingIdReg.Matches(rendered).Select(m => m.Groups[1].Value).ToHashSet();

            foreach (var wordId in wordIds.Except(existingThingIds)) {
                logger.Debug(
                    $"Add word {wordId} to level {levelId}: {new AddWordToLevelRpc {HttpClient = HttpClient}.Call(WebDriver.Url, levelId, wordId).Success}");
            }

            foreach (var wordId in existingThingIds.Except(wordIds)) {
                logger.Debug(
                    $"Remove word {wordId} from level {levelId}: {new RemoveWordFromLevelRpc {HttpClient = HttpClient}.Call(WebDriver.Url, levelId, wordId).Success}");
            }

            logger.Debug(
                $"Reorder words for {levelId}: {new ReorderWordsInLevelRpc {HttpClient = HttpClient}.Call(WebDriver.Url, levelId, wordIds).Success}");
        }

        public KifaActionResult<string> AddWord(GoetheGermanWord word, GermanWord baseWord) {
            WebDriver.Url = Course.DatabaseUrl;

            logger.Debug($"Adding word in {WebDriver.Url}:\n{word}\n{baseWord}");

            // Check headers
            var checkHeadersResult = CheckHeaders(GetHeaders());
            if (checkHeadersResult.Status != KifaActionStatus.OK) {
                return new KifaActionResult<string>(checkHeadersResult);
            }

            var newData = GetDataFromWord(word, baseWord);

            var existingRow = GetExistingRow(word);
            if (existingRow == null) {
                FillBasicWord(newData);
                Thread.Sleep(TimeSpan.FromSeconds(5));
                existingRow = GetExistingRow(word);
                if (existingRow == null) {
                    logger.Error($"Failed to add word: {word.Word}.");
                    return new KifaActionResult<string>(KifaActionStatus.Error, $"failed to add word {word.Word}");
                }
            }

            FillRow(existingRow, newData);

            UploadAudios(existingRow, baseWord);

            return new KifaActionResult<string>(existingRow.ThingId);
        }

        KifaActionResult CheckHeaders(Dictionary<string, string> headers) {
            logger.Debug($"Headers: {string.Join(", ", headers)}");
            foreach (var column in Course.Columns) {
                var headerIndex = headers.GetValueOrDefault(column.Key) ?? "not found";
                if (headerIndex != column.Value) {
                    logger.Fatal(
                        $"Header mismatch: {column.Key} should be in column {column.Value}, not {headerIndex}.");
                    return new KifaActionResult {
                        Status = KifaActionStatus.Error,
                        Message =
                            $"Header mismatch: {column.Key} should be in column {column.Value}, not {headerIndex}."
                    };
                }
            }

            return KifaActionResult.SuccessActionResult;
        }

        void UploadAudios(MemriseWord originalWord, GermanWord baseWord) {
            var currentAudios = GetAudios(originalWord.AudioLinks);

            foreach (var link in baseWord.PronunciationAudioLinks.Where(item => item.Value != null)
                .OrderBy(item => item.Key).SelectMany(item => item.Value).Take(3)) {
                var newAudio = new KifaFile(link).OpenRead().ToByteArray();
                var info = FileInformation.GetInformation(new MemoryStream(newAudio),
                    FileProperties.Size | FileProperties.Md5);
                if (currentAudios.Contains((info.Size ?? 0, info.Md5))) {
                    logger.Debug($"{link} for {baseWord.Id} ({originalWord.ThingId}) already exists.");
                    continue;
                }

                logger.Debug($"Uploading {link} for {baseWord.Id} ({originalWord.ThingId}).");
                new UploadAudioRpc {HttpClient = HttpClient}.Call(WebDriver.Url, originalWord.ThingId, Course.Columns["Audios"],
                    CsrfToken, newAudio);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        HashSet<(long length, string md5)> GetAudios(List<string> currentAudioLinks) {
            var result = new HashSet<(long length, string md5)>();
            foreach (var link in currentAudioLinks) {
                var response = HttpClient.GetHeaders(link);
                result.Add((response.Content.Headers.ContentRange?.Length ?? 0,
                    response.Headers.ETag?.Tag.ToUpperInvariant()[1..^1]));
            }

            return result;
        }

        Dictionary<string, string> GetHeaders() =>
            WebDriver.FindElement(By.CssSelector("thead.columns")).FindElements(By.CssSelector("th.column"))
                .ToDictionary(th => th.Text.Trim(), th => th.GetAttribute("data-key"));

        MemriseWord GetExistingRow(GoetheGermanWord word) {
            return GetExistingRow(word, word.Word) ?? GetExistingRow(word, word.Meaning);
        }

        MemriseWord GetExistingRow(GoetheGermanWord word, string searchQuery) {
            var searchBar = WebDriver.FindElement(By.CssSelector("input#search_string"));
            searchBar.Clear();
            searchBar.SendKeys(searchQuery);
            searchBar.Submit();

            var things = WebDriver.FindElement(By.CssSelector("tbody.things"));
            var rows = things.FindElements(By.CssSelector("tr.thing"));
            if (rows.Count < 1) {
                return null;
            }

            foreach (var row in rows) {
                var cells = row.FindElements(By.TagName("td"));
                if (cells[1].Text == word.Word && cells[2].Text == word.Meaning) {
                    return GetDataFromRow(row);
                }
            }

            return null;
        }

        // Columns order: German, English, Form, Pronunciation, Examples, Audios
        string FillBasicWord(Dictionary<string, string> newData) {
            var response = new AddWordRpc {HttpClient = HttpClient}.Call(Course.DatabaseId, Course.BaseUrl, newData);

            return response.Thing.Id.ToString();
        }

        int FillRow(MemriseWord originalData, Dictionary<string, string> newData) {
            var updatedFields = 0;
            foreach (var (dataKey, newValue) in newData) {
                if (originalData.Data.GetValueOrDefault(dataKey) != newValue) {
                    new UpdateWordRpc {HttpClient = HttpClient}.Call(WebDriver.Url, originalData.ThingId, dataKey,
                        newValue);
                    updatedFields++;
                }
            }

            return updatedFields;
        }

        Dictionary<string, string> GetDataFromWord(GoetheGermanWord word, GermanWord baseWord) {
            var data = new Dictionary<string, string> {
                {Course.Columns["German"], word.Word}, {Course.Columns["English"], word.Meaning}
            };

            if (word.Form != null) {
                data[Course.Columns["Form"]] = word.Form;
            }

            if (baseWord.Pronunciation != null) {
                data[Course.Columns["Pronunciation"]] = $"[{baseWord.Pronunciation}]";
            }

            if (word.Examples.Count > 0 && !word.Examples[0].StartsWith("example")) {
                data[Course.Columns["Examples"]] = string.Join(lineBreak, word.Examples);
            }

            return data;
        }

        MemriseWord GetDataFromRow(IWebElement existingRow) {
            logger.Debug($"Getting word from row {existingRow.Text}.");
            var data = new Dictionary<string, string>();

            foreach (var td in existingRow.FindElements(By.CssSelector("td[data-key]"))) {
                data[td.GetAttribute("data-key")] = td.Text;
            }

            var audioLinks = existingRow.FindElements(By.CssSelector("td[data-key='6'] a"));

            return new() {
                ThingId = existingRow.GetAttribute("data-thing-id"),
                Data = data,
                AudioLinks = audioLinks.Select(link => link.GetAttribute("data-url")).ToList()
            };
        }

        public void Dispose() {
            webDriver?.Dispose();
            httpClient?.Dispose();
        }
    }
}
