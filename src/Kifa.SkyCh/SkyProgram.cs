using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web;
using HtmlAgilityPack;
using Kifa.Service;
using Kifa.SkyCh.Api;
using NLog;

namespace Kifa.SkyCh;

public class SkyProgram : DataModel<SkyProgram> {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string ModelId = "sky.ch/programs";

    public string? Title { get; set; }
    public string? Subtitle { get; set; }

    public List<string>? Categories { get; set; }
    public string? ImageLink { get; set; }

    public string? Channel { get; set; }
    public DateTime AirDateTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Type { get; set; }

    static readonly HttpClient NoAuthClient = new();

    static DateTime lastFilled = DateTime.MinValue;

    // Should not be called frequently.
    public override DateTimeOffset? Fill() {
        WaitCooldown();

        var pageUrl = $"https://sport.sky.ch/en/SkyChannelAjax/DetailEpg?id={Id}";
        var epgPage = NoAuthClient.GetStringAsync(pageUrl).Result;
        var doc = new HtmlDocument();
        doc.LoadHtml(epgPage);
        var root = doc.DocumentNode;

        var imageLinks = root.SelectNodes("//div[@class='img-container']/img");

        if (imageLinks == null) {
            throw new UnableToFillException($"Could not get image link node for {pageUrl}");
        }

        ImageLink = imageLinks[0].Attributes["src"].Value.Split("?")[0];

        // No need to check the following nodes.
        Type = root.SelectSingleNode("//span[@class='type-tag']").InnerText.Trim();
        Title = HttpUtility.HtmlDecode(root.SelectSingleNode("//h1[@class='program-title']")
            .InnerText.Trim());
        Subtitle = HttpUtility.HtmlDecode(root.SelectSingleNode("//h2[@class='program-subtitle']")
            .InnerText.Trim());

        Categories = root.SelectSingleNode("//span[@class='detail'][2]").InnerText.Split(",")
            .Select(s => s.Trim()).ToList();

        var timeStrings =
            root.SelectSingleNode("//time[@class='time']").InnerText.Trim().Split(" ");

        AirDateTime =
            DateTime.ParseExact(
                root.SelectSingleNode("//time[@class='date']").InnerText.Trim() + timeStrings[0],
                "dd.MM.yyyyHH:mm", null);

        Duration = TimeSpan.Parse(timeStrings[2]) - TimeSpan.Parse(timeStrings[0]);
        if (Duration < TimeSpan.Zero) {
            Duration += TimeSpan.FromDays(1);
        }

        Channel = root.SelectSingleNode("//img[@class='channel-logo']").Attributes["alt"].Value;

        return null;
    }

    static void WaitCooldown() {
        var wait = TimeSpan.FromSeconds(10) - (DateTime.Now - lastFilled);
        if (wait > TimeSpan.Zero) {
            Logger.Debug(
                $"SkyProgram.Fill triggered too frequently. Sleep {wait.TotalSeconds} seconds.");
            Thread.Sleep(wait);
        }

        lastFilled = DateTime.Now;
    }

    public string GetVideoLink() => new PlayerRpc().Invoke(Id).Url;
}

public interface SkyProgramServiceClient : KifaServiceClient<SkyProgram> {
    List<SkyProgram> AddForDay(int dayOffset);
}

public class SkyProgramRestServiceClient : KifaServiceRestClient<SkyProgram>,
    SkyProgramServiceClient {
    public List<SkyProgram> AddForDay(int dayOffset) => throw new NotImplementedException();
}
