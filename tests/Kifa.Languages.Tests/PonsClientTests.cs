using Kifa.Languages.German;
using Xunit;

namespace Kifa.Languages.Tests;

public class PonsClientTests {
    [Fact]
    public void VerbFormsTest() {
        var client = new PonsClient();
        var forms = client.GetVerbForms("malen");
        Assert.Equal("male", forms[VerbFormType.IndicativePresent][Person.Ich]);
    }

    [Fact]
    public void VerbTest() {
        var client = new PonsClient();
        var verb = client.GetWord("malen");
        Assert.Equal(WordType.Verb, verb.Type);
        Assert.Equal("to paint", verb.Meaning);
        Assert.Equal("ˈma:lən", verb.Pronunciation);
    }
}
