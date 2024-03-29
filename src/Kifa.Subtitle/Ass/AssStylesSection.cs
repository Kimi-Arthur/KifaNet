﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace Kifa.Subtitle.Ass;

public class AssStylesSection : AssSection {
    public const string SectionHeader = "[V4+ Styles]";
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public override string SectionTitle => SectionHeader;

    public List<string> Format
        => new() {
            "Name",
            "Fontname",
            "Fontsize",
            "PrimaryColour",
            "SecondaryColour",
            "OutlineColour",
            "BackColour",
            "Bold",
            "Italic",
            "Underline",
            "StrikeOut",
            "ScaleX",
            "ScaleY",
            "Spacing",
            "Angle",
            "BorderStyle",
            "Outline",
            "Shadow",
            "Alignment",
            "MarginL",
            "MarginR",
            "MarginV",
            "Encoding"
        };

    // TODO: solve sync problem between these two.
    public List<AssStyle> Styles { get; set; } = new();

    public Dictionary<string, AssStyle> NamedStyles { get; set; } = new();

    public override IEnumerable<AssLine> AssLines {
        get {
            yield return new AssLine("Format", Format);
            foreach (var style in Styles) {
                yield return style;
            }
        }
    }

    public static AssStylesSection Parse(IEnumerable<string> lines) {
        var section = new AssStylesSection();
        List<string> headers = null;
        foreach (var line in lines) {
            var separatorIndex = line.IndexOf(AssLine.Separator, StringComparison.Ordinal);
            if (separatorIndex >= 0) {
                var type = line.Substring(0, separatorIndex);
                var content = line.Substring(separatorIndex + 1).Trim();

                switch (type) {
                    case "Format":
                        headers = content.Split(",").Select(s => s.Trim()).ToList();
                        break;
                    case "Style":
                        if (headers == null) {
                            Logger.Warn(
                                "Should see header line before style line in style section.");
                            break;
                        }

                        try {
                            var style = AssStyle.Parse(content.Split(",").Select(s => s.Trim()),
                                headers);
                            section.NamedStyles[style.Name] = style;
                            section.Styles.Add(style);
                        } catch (Exception ex) {
                            Logger.Error(ex, $"Error parsing style: {content}");
                            throw new Exception($"Error parsing style: {content}", ex);
                        }

                        break;
                }
            }
        }

        return section;
    }
}
