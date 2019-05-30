using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pimix.Subtitle.Ass {
    public class AssLine {
        public const string Separator = ":";

        public AssLine() {
        }

        public AssLine(string key, IEnumerable<string> values) {
            Key = key;
            Values = values;
        }

        public virtual string Key { get; protected set; }
        public virtual IEnumerable<string> Values { get; protected set; }

        public override string ToString()
            => $"{Key}: {string.Join(",", Values.Select(FormatValue))}";

        static string FormatValue(string value) => new Regex("[\n\r]+").Replace(value, @"\n");
    }
}
