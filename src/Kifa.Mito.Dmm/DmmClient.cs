using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using Pimix;

namespace Kifa.Mito.Dmm {
    public class DmmClient {
        const string DmmLink = "https://www.dmm.co.jp/digital/videoa/-/detail/=/cid={dvd_id}/";
        const string R18Link = "https://www.r18.com/videos/vod/movies/detail/-/id={dvd_id}/";
        static string VideoLink => DmmLink;

        HttpClient httpClient = new();

        public void Fill(Video video) {
            video.VideoIds.DmmId = video.Id;
            video.VideoIds.DmmDvdId = GetDvdId(video.Id);
            var doc = new HtmlDocument();
            using var response = httpClient
                .GetAsync(VideoLink.Format(new Dictionary<string, string> {["dvd_id"] = video.VideoIds.DmmDvdId}))
                .Result;

            doc.LoadHtml(response.GetString());

            video.Title = doc.DocumentNode.SelectNodes("//h1[@id='title']").Single().InnerText;

            foreach (var row in doc.DocumentNode.SelectNodes("//tr")) {
                if (row.SelectNodes("./td[1]")?.Single()?.InnerText == "出演者：") {
                    foreach (var actressNode in row.SelectNodes("./td[2]/span[1]/a") ?? Enumerable.Empty<HtmlNode>()) {
                        video.Actresses.Add(new Actress {
                            Id = actressNode.InnerText,
                            Name = actressNode.InnerText,
                            Ids = new JavIds {
                                DmmId = actressNode.GetAttributeValue("href", "0")
                                    .Split("/", StringSplitOptions.RemoveEmptyEntries).Last().Split("=").Last()
                            }
                        });
                    }
                } else if (row.SelectNodes("./td[1]")?.Single()?.InnerText == "ジャンル：") {
                    foreach (var categoryNode in row.SelectNodes("./td[2]/a") ?? Enumerable.Empty<HtmlNode>()) {
                        video.Categories.Add(categoryNode.InnerText);
                    }
                }
            }

            video.Type = video.Categories.Contains("VR専用") ? VideoType.Vr : VideoType.Jav;
            video.Description = doc.DocumentNode.SelectNodes("//div[@class='mg-b20 lh4']").Single().InnerHtml
                .Split("<", 2).First().Trim();

            var actress = video.Actresses.First().Name;
            if (video.Title.EndsWith($" {actress}")) {
                video.Title = video.Title.Substring(0, video.Title.Length - 1 - actress.Length);
            }
        }

        /// <summary>
        /// Converts video id to dvd id used by DMM. Normally it's lowercased, hyphen-removed
        /// and pads with 0.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static string GetDvdId(string id) {
            var parts = id.Split("-");
            return parts[0].ToLower() + parts[1].PadLeft(5, '0');
        }
    }
}
