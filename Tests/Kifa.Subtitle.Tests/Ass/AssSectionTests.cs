using System.Collections.Generic;
using Kifa.Subtitle.Ass;
using Xunit;

namespace Kifa.Subtitle.Tests.Ass;

public class AssSectionTests {
    [Fact]
    public void ParseScriptInfoSectionTest() {
        var lines = new List<string> {
            "Title: Test Title",
            "Original Script: Test Author"
        };
        var section = AssSection.Parse(null, "[Script Info]", lines);
        Assert.NotNull(section);
        Assert.IsType<AssScriptInfoSection>(section);

        var scriptInfo = Assert.IsType<AssScriptInfoSection>(section);
        Assert.Equal("Test Title", scriptInfo.Title);
        Assert.Equal("Test Author", scriptInfo.OriginalScript);
    }

    [Fact]
    public void ParseStylesSectionTest() {
        var lines = new List<string> {
            "Format: Name, Fontname, Fontsize",
            "Style: Default, Arial, 20"
        };
        var section = AssSection.Parse(null, "[V4+ Styles]", lines);
        Assert.NotNull(section);
        var styles = Assert.IsType<AssStylesSection>(section);
        Assert.Single(styles.Styles);
        Assert.Equal("Default", styles.Styles[0].Name);
    }

    [Fact]
    public void ParseEventsSectionTest() {
        var lines = new List<string> {
            "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text",
            "Dialogue: 0,0:00:00.00,0:00:05.00,Default,,0,0,0,,Hello World"
        };
        var section = AssSection.Parse(null, "[Events]", lines);
        Assert.NotNull(section);
        var events = Assert.IsType<AssEventsSection>(section);
        Assert.Single(events.Events);
        Assert.Equal("Hello World", events.Events[0].Text?.ToString());
    }

    [Fact]
    public void ParseUnknownSectionTest() {
        var section = AssSection.Parse(null, "[Unknown Section]", new List<string>());
        Assert.Null(section);
    }
}
