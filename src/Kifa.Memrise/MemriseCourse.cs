using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Service;
using Newtonsoft.Json;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using YamlDotNet.Serialization;

namespace Kifa.Memrise;

public class MemriseCourse : DataModel<MemriseCourse> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string ModelId = "memrise/courses";

    public string CourseName { get; set; }
    public string CourseId { get; set; }
    public string DatabaseId { get; set; }

    // Map from column name to data-key.
    [JsonIgnore]
    public Dictionary<string, string> Columns { get; set; }

    // Map from level name to its id. The name doesn't have to comply with the actual level name.
    public Dictionary<string, string> Levels { get; set; }

    public Dictionary<string, Link<MemriseWord>> Words { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string DatabaseUrl => $"{BaseUrl}database/{DatabaseId}/";

    [JsonIgnore]
    [YamlIgnore]
    public string BaseUrl => $"https://app.memrise.com/course/{CourseId}/{CourseName}/edit/";

    
    static IWebDriver webDriver;

    static IWebDriver WebDriver {
        get {
            if (webDriver == null) {
                var options = new ChromeOptions();
                //options.AddArgument("--headless");
                webDriver = new RemoteWebDriver(new Uri(MemriseClient.WebDriverUrl), options.ToCapabilities(),
                    TimeSpan.FromMinutes(10));

                webDriver.Url = "https://app.memrise.com/";

                foreach (var cookie in MemriseClient.Cookies.Split("; ")) {
                    var cookiePair = cookie.Split("=", 2);
                    webDriver.Manage().Cookies.AddCookie(new Cookie(cookiePair[0], cookiePair[1],
                        "app.memrise.com", "/", DateTime.Now + TimeSpan.FromDays(365)));
                }
            }

            return webDriver;
        }
    }

    public override DateTimeOffset? Fill() {
        WebDriver.Url = DatabaseUrl;

        var totalPageNumber =
            int.Parse(WebDriver.FindElements(By.CssSelector("ul.pagination > li"))[^2].Text);

        var words = new Dictionary<string, MemriseWord>();
        for (var i = 0; i < totalPageNumber; i++) {
            WebDriver.Url = $"{DatabaseUrl}?page={i + 1}";
            GetWordsInPage().ForEach(word => words.Add(word.Data[Columns["German"]], word));
        }

        return Date.Zero;
    }

    public List<MemriseWord> GetWordsInPage() {
        var things = WebDriver.FindElement(By.CssSelector("tbody.things"));
        var rows = things.FindElements(By.CssSelector("tr.thing"));
        return rows.Select(GetDataFromRow).ToList();
    }

    public MemriseWord GetDataFromRow(IWebElement existingRow) {
        Logger.Trace($"Getting word from row {existingRow.Text}.");
        var data = new Dictionary<string, string>();

        foreach (var td in existingRow.FindElements(By.CssSelector("td[data-key]"))) {
            data[td.GetAttribute("data-key")] = td.Text;
        }

        var audioLinks =
            existingRow.FindElements(By.CssSelector($"td[data-key='{Columns["Audios"]}'] a"));

        return new MemriseWord {
            Id = existingRow.GetAttribute("data-thing-id"),
            Data = data,
            Audios = audioLinks.Select(link => new MemriseAudio {
                Link = link.GetAttribute("data-url")
            }).ToList()
        };
    }
}

public interface MemriseCourseServiceClient : KifaServiceClient<MemriseCourse> {
}

public class MemriseCourseRestServiceClient : KifaServiceRestClient<MemriseCourse>,
    MemriseCourseServiceClient {
}
