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

        public static string GetDesiredFileName(string aid, int pid, string cid = null) {
            PimixService.Update(new BilibiliVideo {Id = aid});
            var v = PimixService.Get<BilibiliVideo>(aid);
            var p = v.Pages.First(x => x.Id == pid);

            if (cid != null && cid != p.Cid) {
                return null;
            }

            return v.Pages.Count > 1
                ? $"{v.Title} P{pid} {p.Title}-{aid}p{pid}.c{cid}"
                : $"{v.Title} {p.Title}-{aid}.c{cid}";
        }
    }
}
