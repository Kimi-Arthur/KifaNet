using FluentAssertions;
using Kifa.Languages.Oxford;
using Xunit;

namespace Kifa.Languages.Tests;

public class OxfordTests {
    [Fact]
    public void PageLoadTest() {
        var page = new OxfordPage {
            Id = "kill_1"
        };
        page.Fill();
        page.PageContent.Should().Contain("Cancer kills thousands of people every year");
        page.PagesBefore.Should().BeEquivalentTo(["kikoi", "kilim"]);
        page.PagesAfter.Should().BeEquivalentTo(["kill_2", "killer"]);
    }
}
