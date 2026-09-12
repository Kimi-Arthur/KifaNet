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
                                 Error =>
                                 	errorchild: Error =>
                                 		first: OK
                                 		second: Error => Unknown error
                                 	pendingchild: Pending =>
                                 		first: OK => success message
                                 		second: Pending => what message
                                 """);
    }

    [Fact]
    public void SerializationTest() {
        var fromValue = "{\"status\":\"OK\",\"value\":\"test_value\"}"
            .FromJson<KifaActionResult<string>>();
        fromValue.Should().NotBeNull();
        fromValue!.Status.Should().Be(KifaActionStatus.OK);
        fromValue.Value.Should().Be("test_value");

        var serialized = new KifaActionResult<string>("hello").ToJson();
        serialized.Should().Contain("\"value\":\"hello\"");

        var errorResult = "{\"status\":\"error\",\"message\":\"something failed\"}"
            .FromJson<KifaActionResult<string>>();
        errorResult.Should().NotBeNull();
        errorResult!.Status.Should().Be(KifaActionStatus.Error);
        errorResult.Message.Should().Be("something failed");
        errorResult.Value.Should().BeNull();
    }

    [Fact]
    public void BatchStatusResolutionTest() {
        new KifaBatchActionResult().Status.Should().Be(KifaActionStatus.Skipped);

        new KifaBatchActionResult([
            ("a", KifaActionResult.Skipped()),
            ("b", KifaActionResult.Skipped())
        ]).Status.Should().Be(KifaActionStatus.Skipped);

        new KifaBatchActionResult([
            ("a", KifaActionResult.Success()),
            ("b", KifaActionResult.Skipped())
        ]).Status.Should().Be(KifaActionStatus.OK);

        new KifaBatchActionResult([
            ("a", KifaActionResult.Warning()),
            ("b", KifaActionResult.Skipped())
        ]).Status.Should().Be(KifaActionStatus.Warning);

        new KifaBatchActionResult([
            ("a", KifaActionResult.Skipped()),
            ("b", KifaActionResult.Error())
        ]).Status.Should().Be(KifaActionStatus.Error);

        new KifaBatchActionResult([
            ("a", KifaActionResult.Success()),
            ("b", KifaActionResult.Error())
        ]).Status.Should().Be(KifaActionStatus.Error);

        new KifaBatchActionResult([
            ("a", KifaActionResult.Success()),
            ("b", new KifaActionResult { Status = KifaActionStatus.Pending })
        ]).Status.Should().Be(KifaActionStatus.Pending);
    }
}
