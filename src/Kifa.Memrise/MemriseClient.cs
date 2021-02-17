using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Kifa.Memrise {
    public class MemriseClient {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string WebDriverUrl { get; set; }
        public static string Cookies { get; set; }

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
                    httpClient.DefaultRequestHeaders.Add("x-csrftoken",
                        "LpeIgjFbA8N8hMCP9kJI6gCUOVoErxV2EHcCnWuiTq5j8JU8f6fdSXILA1L9jCEn");
                    httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
                    httpClient.DefaultRequestHeaders.Add("referer", DatabaseUrl);
                }

                return httpClient;
            }
        }

        public void AddWord(MemriseGermanWord word) {
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
        }

        List<string> GetHeaders() =>
            WebDriver.FindElement(By.CssSelector("thead.columns")).FindElements(By.CssSelector("th.column"))
                .Select(th => th.Text.Trim()).ToList();

        IWebElement GetExistingRow(MemriseGermanWord word) {
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

        string FillBasicWord(MemriseGermanWord word) {
            var response = HttpClient.PostAsync("https://app.memrise.com/ajax/thing/add/",
                new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>> {
                    new("columns",
                        JsonConvert.SerializeObject(new Dictionary<string, string> {
                            {"1", word.Word}, {"2", word.Meaning}
                        })),
                    new("pool_id", DatabaseId)
                })).Result;
            if (response.IsSuccessStatusCode) {
                var result = response.GetJToken();
                return result["thing"]["id"].ToString();
            }

            return null;
        }

        void FillRow(IWebElement existingRow, MemriseGermanWord word) {
        }
    }
}
