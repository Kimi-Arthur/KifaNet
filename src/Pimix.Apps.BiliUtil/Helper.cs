using System;
using System.Linq;
using System.Text.RegularExpressions;
using Pimix.Bilibili;
using Pimix.Service;

namespace Pimix.Apps.BiliUtil {
    public class Helper {
        static readonly Regex fileNamePattern = new Regex(@"^AV(\d+) P(\d+) .* cid (\d+)$");

        public static (string aid, int pid, string cid) GetIds(string name) {
            var match = fileNamePattern.Match(name);
            if (!match.Success) {
                return (null, 0, null);
            }

            return ($"av{match.Groups[1].Value}", Int32.Parse(match.Groups[2].Value),
                match.Groups[3].Value);
        }

        public static string GetDesiredFileName(BilibiliVideo video, int pid, string cid = null) {
            var p = video.Pages.First(x => x.Id == pid);

            if (cid != null && cid != p.Cid) {
                return null;
            }

            return video.Pages.Count > 1
                ? $"{video.Author}-{video.AuthorId}/{video.Title} P{pid} {p.Title}-{video.Id}p{pid}.c{cid}"
                : $"{video.Author}-{video.AuthorId}/{video.Title} {p.Title}-{video.Id}.c{cid}";
        }
    }
}
