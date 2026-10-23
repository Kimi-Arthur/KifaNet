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
    public void SubResultsColoringTest() {
        try {
            ConsoleColorExtensions.ForceEnabled = true;
            var r = new KifaBatchActionResult([
                ("file1", KifaActionResult.Success()),
                ("file2", KifaActionResult.Error("failed")),
                ("file3", KifaActionResult.Warning("warn")),
                ("file4", KifaActionResult.Skipped()),
                ("file5", new KifaActionResult { Status = KifaActionStatus.Pending }),
                ("file6", KifaActionResult.Cancelled("cancelled")),
                ("file7", KifaActionResult.BadRequest("bad input"))
            ]);

            r.ToString().Should().Be(
                "\u001b[35mError\u001b[0m =>\n" +
                "\tfile1: \u001b[32mOK\u001b[0m\n" +
                "\tfile2: \u001b[35mError\u001b[0m => \u001b[35mfailed\u001b[0m\n" +
                "\tfile3: \u001b[33mWarning\u001b[0m => \u001b[33mwarn\u001b[0m\n" +
                "\tfile4: \u001b[37mSkipped\u001b[0m\n" +
                "\tfile5: \u001b[97mPending\u001b[0m\n" +
                "\tfile6: \u001b[33mCancelled\u001b[0m => \u001b[33mcancelled\u001b[0m\n" +
                "\tfile7: \u001b[35mBadRequest\u001b[0m => \u001b[35mbad input\u001b[0m"
            );
        } finally {
            ConsoleColorExtensions.ForceEnabled = null;
        }
    }

    [Fact]
    public void SubResultsNestedColoringTest() {
        try {
            ConsoleColorExtensions.ForceEnabled = true;
            var r = new KifaBatchActionResult([
                ("errorchild", new KifaBatchActionResult([
                    ("first", KifaActionResult.Success()),
                    ("second", KifaActionResult.UnknownError())
                ])),
                ("pendingchild", new KifaBatchActionResult([
                    ("first", KifaActionResult.Success("success message")),
                    ("second", new KifaActionResult {
                        Status = KifaActionStatus.Pending,
                        Message = "what message"
                    })
                ]))
            ]);

            r.ToString().Should().Be(
                "\u001b[35mError\u001b[0m =>\n" +
                "\terrorchild: \u001b[35mError\u001b[0m =>\n" +
                "\t\tfirst: \u001b[32mOK\u001b[0m\n" +
                "\t\tsecond: \u001b[35mError\u001b[0m => \u001b[35mUnknown error\u001b[0m\n" +
                "\tpendingchild: \u001b[97mPending\u001b[0m =>\n" +
                "\t\tfirst: \u001b[32mOK\u001b[0m => \u001b[32msuccess message\u001b[0m\n" +
                "\t\tsecond: \u001b[97mPending\u001b[0m => \u001b[97mwhat message\u001b[0m"
            );
        } finally {
            ConsoleColorExtensions.ForceEnabled = null;
        }
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
    public void BatchSerializationTest() {
        var batch = new KifaBatchActionResult([
            ("file1", KifaActionResult.Success()),
            ("file2", KifaActionResult.Error("conflicting values for Locations"))
        ]);

        var json = batch.ToJson();
        json.Should().Contain("\"item\":\"file1\"");
        json.Should().Contain("\"item\":\"file2\"");

        var deserialized = json.FromJson<KifaBatchActionResult>();
        deserialized.Should().NotBeNull();
        deserialized!.Status.Should().Be(KifaActionStatus.Error);
        deserialized.Results.Should().HaveCount(2);
        deserialized.Results[0].Item.Should().Be("file1");
        deserialized.Results[0].Result.Status.Should().Be(KifaActionStatus.OK);
        deserialized.Results[1].Item.Should().Be("file2");
        deserialized.Results[1].Result.Status.Should().Be(KifaActionStatus.Error);
        deserialized.Results[1].Result.Message.Should().Be("conflicting values for Locations");

        deserialized.ToString().Should().Be("""
                                            Error =>
                                            	file1: OK
                                            	file2: Error => conflicting values for Locations
                                            """);
    }

    [Fact]
    public void BatchEmptyResultsToStringTest() {
        var emptyBatch = new KifaBatchActionResult();
        emptyBatch.ToString().Should().Be("Skipped");

        var batchWithMessage = new KifaBatchActionResult {
            Status = KifaActionStatus.OK,
            Message = "Done"
        };
        batchWithMessage.ToString().Should().Be("OK => Done");
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

        new KifaBatchActionResult([
            ("a", KifaActionResult.Cancelled()),
            ("b", KifaActionResult.Skipped())
        ]).Status.Should().Be(KifaActionStatus.Cancelled);

        new KifaBatchActionResult([
            ("a", KifaActionResult.Success()),
            ("b", KifaActionResult.Cancelled())
        ]).Status.Should().Be(KifaActionStatus.Cancelled);
    }

    [Fact]
    public void CancelledPropertiesTest() {
        var res = KifaActionResult.Cancelled("cancelled by user");
        res.Status.Should().Be(KifaActionStatus.Cancelled);
        res.Message.Should().Be("cancelled by user");
        res.IsAcceptable.Should().BeFalse();
        res.IsRetryable.Should().BeTrue();

        var resT = KifaActionResult<string>.Cancelled("cancelled");
        resT.Status.Should().Be(KifaActionStatus.Cancelled);
        resT.Message.Should().Be("cancelled");
    }
}
