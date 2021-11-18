using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Kifa.SkyCh;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Soccer {
    [Route("api/" + SkyProgram.ModelId)]
    public class SkyProgramsController : KifaDataController<SkyProgram, SkyProgramJsonServiceClient> {
        [HttpGet("$add_for_day")]
        public KifaApiActionResult<List<SkyProgram>> AddForDay(int dayOffset) => Client.AddForDay(dayOffset);
    }

    public class SkyProgramJsonServiceClient : KifaServiceJsonClient<SkyProgram>, SkyProgramServiceClient {
        static readonly HttpClient NoAuthClient = new();

        public List<SkyProgram> AddForDay(int dayOffset) {
            var channels = Channels;
            var date = DateTime.UtcNow.Date.AddDays(dayOffset);
            var listPage = NoAuthClient
                .GetStringAsync($"https://sport.sky.ch/en/SkyChannelAjax/UpdateEpg?day={dayOffset}").Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(listPage);
            var channelNodes = doc.DocumentNode.SelectNodes("//ul");
            if (channelNodes.Count != channels.Count) {
                RefreshChannels();
                if (channelNodes.Count != channels.Count) {
                    return new List<SkyProgram>();
                }
            }

            var programs = new List<SkyProgram>();
            foreach (var (channelNode, channelName) in channelNodes.Zip(channels)) {
                foreach (var programNode in channelNode.SelectNodes(".//li")
                    .Where(node => node.Attributes["data-id"].Value != "0")) {
                    var textNodes = programNode.SelectNodes(".//div[@class='text-container']/p");
                    var imageNodes = programNode.SelectNodes(".//div[@class='img-container']");

                    var timeStrings = textNodes[2].InnerText.Trim().Split(" ");

                    var duration = TimeSpan.Parse(timeStrings[2]) - TimeSpan.Parse(timeStrings[0]);
                    if (duration < TimeSpan.Zero) {
                        duration += TimeSpan.FromDays(1);
                    }

                    var startTime = TimeSpan.Parse(timeStrings[0]);
                    if (programNode.Attributes["data-previous"].Value == "-1" && startTime > TimeSpan.FromHours(12)) {
                        startTime -= TimeSpan.FromDays(1);
                    }

                    var program = new SkyProgram {
                        Id = programNode.Attributes["data-id"].Value,
                        Title = HttpUtility.HtmlDecode(textNodes[0].InnerText.Trim()),
                        Subtitle = HttpUtility.HtmlDecode(textNodes[1].InnerText.Trim()),
                        ImageLink = imageNodes?.Count == 1
                            ? ParseBackgroundImageLink(imageNodes[0].Attributes["style"].Value)
                            : null,
                        Channel = channelName,
                        AirDateTime = date + startTime,
                        Duration = duration
                    };

                    Set(program);
                    programs.Add(program);
                }
            }

            return programs;
        }

        static readonly Regex backgroundImageLinkRegex = new Regex(@"(https://.*)\?");
        static string ParseBackgroundImageLink(string style) => backgroundImageLinkRegex.Match(style).Groups[1].Value;

        static List<string>? channels;
        public static List<string> Channels => channels ??= FetchChannels();

        public static List<string> FetchChannels() {
            var channelsPage = NoAuthClient.GetStringAsync("https://sport.sky.ch/en/live-of-tv").Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(channelsPage);
            var nodes = doc.DocumentNode.SelectNodes("//li[@class='epg-channel-list-item']");
            return nodes.Select(node => node.SelectSingleNode(".//img").Attributes["alt"].Value).ToList();
        }

        static void RefreshChannels() {
            channels = FetchChannels();
        }
    }
}
