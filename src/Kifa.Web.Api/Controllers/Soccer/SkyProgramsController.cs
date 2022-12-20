using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Kifa.SkyCh;
using Microsoft.AspNetCore.Mvc;

namespace Kifa.Web.Api.Controllers.Soccer;


public class SkyProgramsController : KifaDataController<SkyLiveProgram, SkyProgramJsonServiceClient> {
    [HttpGet("$add_for_day")]
    public KifaApiActionResult<List<SkyLiveProgram>> AddForDay(int dayOffset)
        => Client.AddForDay(dayOffset);
}

public class SkyProgramJsonServiceClient : KifaServiceJsonClient<SkyLiveProgram>,
    SkyProgramServiceClient {
    static readonly HttpClient NoAuthClient = new();

    public List<SkyLiveProgram> AddForDay(int dayOffset)
        => AddForDayAndLanguage(dayOffset, "de").Concat(AddForDayAndLanguage(dayOffset, "en"))
            .ToList(); // The order will make en version more possible to stay.

    public List<SkyLiveProgram> AddForDayAndLanguage(int dayOffset, string language) {
        var channels = Channels[language];
        var date = DateTime.UtcNow.Date.AddDays(dayOffset);
        var listPage = NoAuthClient
            .GetStringAsync(
                $"https://sport.sky.ch/{language}/SkyChannelAjax/UpdateEpg?day={dayOffset}").Result;
        var doc = new HtmlDocument();
        doc.LoadHtml(listPage);
        var channelNodes = doc.DocumentNode.SelectNodes("//ul");

        var programs = new List<SkyLiveProgram>();
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
                if (programNode.Attributes["data-previous"].Value == "-1" &&
                    startTime > TimeSpan.FromHours(12)) {
                    startTime -= TimeSpan.FromDays(1);
                }

                if (programs.Count > 0 &&
                    programNode.Attributes["data-id"].Value == programs[^1].Id) {
                    var program = programs[^1].With(p => {
                        p.Duration += duration;
                        p.Title = MergeTitle(p.Title);
                        p.Subtitle = MergeTitle(p.Subtitle);
                    });

                    Set(program);
                } else {
                    var program = new SkyLiveProgram {
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
        }

        return programs;
    }

    static readonly Regex PartRegex = new(@" \((Part|Parte|Partie|Teil) .*\)$");
    static string MergeTitle(string title) => PartRegex.Replace(title, "");

    static readonly Regex backgroundImageLinkRegex = new(@"(https://.*)\?");

    static string ParseBackgroundImageLink(string style)
        => backgroundImageLinkRegex.Match(style).Groups[1].Value;

    static Dictionary<string, List<string>>? channels;

    public static Dictionary<string, List<string>> Channels
        => channels ??= new Dictionary<string, List<string>> {
            { "en", FetchChannelsForLanguage("https://sport.sky.ch/en/live-of-tv") },
            { "de", FetchChannelsForLanguage("https://sport.sky.ch/de/live-auf-tv") },
            { "it", FetchChannelsForLanguage("https://sport.sky.ch/it/in-diretta-sulla-TV") },
            { "fr", FetchChannelsForLanguage("https://sport.sky.ch/fr/en-direct-a-la-tv") }
        };

    public static List<string> FetchChannelsForLanguage(string page) {
        var channelsPage = NoAuthClient.GetStringAsync(page).Result;
        var doc = new HtmlDocument();
        doc.LoadHtml(channelsPage);
        var nodes = doc.DocumentNode.SelectNodes("//li[@class='epg-channel-list-item']");
        return nodes.Select(node => node.SelectSingleNode(".//img").Attributes["alt"].Value)
            .ToList();
    }
}
