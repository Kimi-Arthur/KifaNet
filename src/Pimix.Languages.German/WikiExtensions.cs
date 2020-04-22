using System.Linq;
using MwParserFromScratch.Nodes;

namespace Pimix.Languages.German {
    public static class WikiExtensions {
        public static string GetTitle(this Heading heading) {
            return heading.Inlines.First().ToPlainText();
        }
    }
}
