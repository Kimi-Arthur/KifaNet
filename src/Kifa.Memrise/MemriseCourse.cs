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

    static MemriseCourseServiceClient? client;

    public static MemriseCourseServiceClient Client {
        get => client ??= new MemriseCourseRestServiceClient();
        set => client = value;
    }

    static MemriseWordServiceClient WordClient => MemriseWord.Client;

    public const string ModelId = "memrise/courses";

    public string CourseName { get; set; }
    public string CourseId { get; set; }
    public string DatabaseId { get; set; }

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
                options.AddArgument("--headless");
                webDriver = new RemoteWebDriver(new Uri(MemriseClient.WebDriverUrl),
                    options.ToCapabilities(), TimeSpan.FromMinutes(10));

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
        FillHeaders();

        WebDriver.Url = DatabaseUrl;

        var elements = WebDriver.FindElements(By.CssSelector("ul.pagination > li > a"));
        var totalPageNumber = elements.Where(element => element.GetDomAttribute("href") != "#")
            .Select(element => int.Parse(element.GetDomAttribute("href")[6..])).Max();

        // Should reuse the previous words maybe.
        Words = new Dictionary<string, Link<MemriseWord>>();

        for (var i = 0; i < totalPageNumber; i++) {
            Logger.Debug($"Filling page {i + 1}...");
            Retry.Run(() => {
                WebDriver.Url = $"{DatabaseUrl}?page={i + 1}";
                foreach (var word in GetWordsInPage()) {
                    var oldWord = WordClient.Get(word.Id);
                    if (word.Audios != null) {
                        if (oldWord?.Audios != null) {
                            foreach (var audio in word.Audios) {
                                var existingAudio =
                                    oldWord.Audios.FirstOrDefault(a => a.Link == audio.Link);
                                if (existingAudio != null) {
                                    audio.Md5 = existingAudio.Md5;
                                    audio.Size = existingAudio.Size;
                                }
                            }
                        }

                        word.FillAudios();
                    }

                    Words.Add(word.Data[Columns["German"]], new Link<MemriseWord>(word));
                    WordClient.Set(word);
                }
            }, (ex, index) => {
                if (index > 5 || ex is not StaleElementReferenceException) {
                    throw ex;
                }

                Logger.Warn(ex, $"Failed to get data for page {i + 1} ({index}).");
            });
        }

        return null;
    }

    public void FillHeaders() {
        WebDriver.Url = DatabaseUrl;
        Columns = WebDriver.FindElement(By.CssSelector("thead.columns"))
            .FindElements(By.CssSelector("th.column")).ToDictionary(th => th.Text.Trim(),
                th => th.GetAttribute("data-key"));
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
    void AddWord(string courseId, MemriseWord word);
}

public class AddWordRequest {
    public string Id { get; set; }
    public MemriseWord Word { get; set; }
}

public class MemriseCourseRestServiceClient : KifaServiceRestClient<MemriseCourse>,
    MemriseCourseServiceClient {
    public void AddWord(string courseId, MemriseWord word) => Call("add_word", new AddWordRequest {
        Id = courseId,
        Word = word
    });
}
