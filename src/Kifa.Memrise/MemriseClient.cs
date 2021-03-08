using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Kifa.Api.Files;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise.Api;
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

        public string CourseId { get; set; }

        public string CourseName { get; set; }

        public string DatabaseId { get; set; }

        string DatabaseUrl => $"https://app.memrise.com/course/{CourseId}/{CourseName}/edit/database/{DatabaseId}/";

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
                    httpClient.DefaultRequestHeaders.Add("referer", DatabaseUrl);
                }

                return httpClient;
            }
        }

        public void AddWord(GoetheGermanWord word, Word baseWord) {
            WebDriver.Url = DatabaseUrl;

            logger.Debug($"Adding word in {WebDriver.Url}:\n{word}");

            var headers = GetHeaders();
            logger.Debug($"Headers: {string.Join(", ", headers)}");
            var existingRow = GetExistingRow(word);
            if (existingRow == null) {
                FillBasicWord(word);
                existingRow = GetExistingRow(word);
            }

            FillRow(existingRow, word);

            UploadAudios(existingRow, baseWord);
        }

        void UploadAudios(IWebElement existingRow, Word baseWord) {
            var (thingId, originalData) = GetDataFromRow(existingRow);

            // TODO: Check if audio is already there.
            foreach (var link in baseWord.PronunciationAudioLinks.OrderBy(item => item.Key)
                .SelectMany(item => item.Value).Take(3)) {
                new UploadAudioRpc {HttpClient = HttpClient}.Call(WebDriver.Url, thingId, "7", CsrfToken,
                    new KifaFile(link).OpenRead().ToByteArray());
            }
        }

        List<string> GetHeaders() =>
            WebDriver.FindElement(By.CssSelector("thead.columns")).FindElements(By.CssSelector("th.column"))
                .Select(th => th.Text.Trim()).ToList();

        IWebElement GetExistingRow(GoetheGermanWord word) {
            var searchBar = WebDriver.FindElement(By.CssSelector("input#search_string"));
            searchBar.Clear();
            searchBar.SendKeys(word.Word);
            searchBar.Submit();

            var things = WebDriver.FindElement(By.CssSelector("tbody.things"));
            var rows = things.FindElements(By.CssSelector("tr.thing"));
            if (rows.Count < 1) {
                return null;
            }

            foreach (var row in rows) {
                var cells = row.FindElements(By.TagName("td"));
                if (cells[1].Text == word.Word && cells[2].Text == word.Meaning) {
                    return row;
                }
            }

            return null;
        }

        // Columns order: German, English, Form, Pronunciation, Full Form, Examples, Audio
        string FillBasicWord(GoetheGermanWord word) {
            var response =
                new AddWordRpc {HttpClient = HttpClient}.Call(DatabaseId, DatabaseUrl, GetDataFromWord(word));

            return response.Thing.Id.ToString();
        }

        int FillRow(IWebElement existingRow, GoetheGermanWord word) {
            var (thingId, originalData) = GetDataFromRow(existingRow);
            var newData = GetDataFromWord(word);

            var updatedFields = 0;
            foreach (var (dataKey, newValue) in newData) {
                if (originalData.GetValueOrDefault(dataKey) != newValue) {
                    new UpdateWordRpc {HttpClient = HttpClient}.Call(WebDriver.Url, thingId, dataKey, newValue);
                    updatedFields++;
                }
            }

            return updatedFields;
        }

        Dictionary<string, string> GetDataFromWord(GoetheGermanWord word) {
            var data = new Dictionary<string, string> {{"1", word.Word}, {"2", word.Meaning}};

            if (word.Form != null) {
                data["3"] = word.Form;
            }

            if (!word.Examples[0].StartsWith("example")) {
                data["6"] = string.Join(lineBreak, word.Examples);
            }

            return data;
        }

        (string thingId, Dictionary<string, string> data) GetDataFromRow(IWebElement existingRow) {
            var data = new Dictionary<string, string>();

            foreach (var td in existingRow.FindElements(By.CssSelector("td[data-key]"))) {
                data[td.GetAttribute("data-key")] = td.Text;
            }

            return (existingRow.GetAttribute("data-thing-id"), data);
        }

        public void Dispose() {
            webDriver?.Dispose();
            httpClient?.Dispose();
        }
    }
}
