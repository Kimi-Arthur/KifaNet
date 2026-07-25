using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kifa.Subtitle.Ass;

public abstract class AssSection {
    public abstract string SectionTitle { get; }

    public virtual IEnumerable<AssLine> AssLines => new List<AssLine>();

    public override string ToString()
        => $"{SectionTitle}\n{string.Join("\n", AssLines.Select(line => line.ToString()))}\n";

    static Dictionary<string, Func<AssStylesSection?, IEnumerable<string>, AssSection>> Parsers
        => field ??= typeof(AssSection).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(AssSection))).Select(t => (
                Header: (string) t.GetProperty(nameof(AssSection<>.SectionHeader))!.GetValue(null)!,
                Parser: t.GetMethod(nameof(AssSection<>.Parse),
                        [typeof(AssStylesSection), typeof(IEnumerable<string>)])!
                    .CreateDelegate<Func<AssStylesSection?, IEnumerable<string>, AssSection>>()))
            .ToDictionary(x => x.Header, x => x.Parser);

    public static AssSection? Parse(AssStylesSection? stylesSection, string title,
        IEnumerable<string> lines)
        => Parsers.TryGetValue(title, out var parser) ? parser(stylesSection, lines) : null;
}

public interface AssSection<TSelf> where TSelf : AssSection, AssSection<TSelf> {
    static abstract string SectionHeader { get; }

    static abstract TSelf Parse(AssStylesSection? stylesSection, IEnumerable<string> lines);
}
