using Xunit;
using FluentAssertions;

namespace Kifa.Service.Tests;

public class KifaActionResultTests {
    [Fact]
    public void SubReultsTest() {
        var r = new KifaBatchActionResult([
            ("errorchild", new KifaBatchActionResult([
                ("first", KifaActionResult.Success()), ("second", KifaActionResult.UnknownError())
            ])),
            ("pendingchild", new KifaBatchActionResult([
                ("first", KifaActionResult.Success("success message")), ("second",
                    new KifaActionResult {
                        Status = KifaActionStatus.Pending,
                        Message = "what message"
                    })
            ]))
        ]);
        r.ToString().Should().Be("""
                                 Error, Pending =>
                                 	errorchild: Error =>
                                 		first: OK
                                 		second: Error => Unknown error
                                 	pendingchild: Pending =>
                                 		first: OK => success message
                                 		second: Pending => what message
                                 """);
    }
}
