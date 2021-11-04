using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using HtmlAgilityPack;
using Kifa.Service;
using Kifa.SkyCh.Api;

namespace Kifa.SkyCh {
    public class SkyProgram : DataModel<SkyProgram> {
        public const string ModelId = "sky.ch/programs";

        public string Title { get; set; }
        public string Subtitle { get; set; }

        public List<string> Categories { get; set; }
        public string ImageLink { get; set; }

        public string Channel { get; set; }
        public DateTime AirDateTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Type { get; set; }

        static readonly HttpClient NoAuthClient = new HttpClient();

        public override bool? Fill() {
            var epgPage = NoAuthClient.GetStringAsync($"https://sport.sky.ch/en/SkyChannelAjax/DetailEpg?id={Id}")
                .Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(epgPage);
            var root = doc.DocumentNode;
            ImageLink = root.SelectSingleNode("//div[@class='img-container']/img").Attributes["src"].Value
                .Split("?")[0];

            Type = root.SelectSingleNode("//span[@class='type-tag']").InnerText.Trim();
            Title = HttpUtility.HtmlDecode(root.SelectSingleNode("//h1[@class='program-title']").InnerText.Trim());
            Subtitle = HttpUtility.HtmlDecode(root.SelectSingleNode("//h2[@class='program-subtitle']").InnerText.Trim());

            Categories = root.SelectSingleNode("//span[@class='detail'][2]").InnerText.Split(",").Select(s => s.Trim())
                .ToList();

            var timeStrings = root.SelectSingleNode("//time[@class='time']").InnerText.Trim().Split(" ");

            AirDateTime =
                DateTime.ParseExact(root.SelectSingleNode("//time[@class='date']").InnerText.Trim() + timeStrings[0],
                    "dd.MM.yyyyHH:mm", null);

            Duration = TimeSpan.Parse(timeStrings[2]) - TimeSpan.Parse(timeStrings[0]);
            if (Duration < TimeSpan.Zero) {
                Duration += TimeSpan.FromDays(1);
            }

            Channel = root.SelectSingleNode("//img[@class='channel-logo']").Attributes["alt"].Value;

            return true;
        }

        public string GetVideoLink() => new PlayerRpc().Call(Id).Url;
    }

    public interface SkyProgramServiceClient : KifaServiceClient<SkyProgram> {
    }

    public class SkyProgramRestServiceClient : KifaServiceRestClient<SkyProgram>, SkyProgramServiceClient {
    }
}
