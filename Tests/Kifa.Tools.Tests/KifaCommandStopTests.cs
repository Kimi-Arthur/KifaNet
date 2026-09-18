using System;
using System.Collections.Generic;
using Kifa.Jobs;
using Kifa.Service;
using Xunit;

namespace Kifa.Tools.Tests;

public class KifaCommandStopTests {
    class TestCommand : KifaCommand {
        public override int Execute(KifaTask? task = null) => 0;

        public void RunItemAction(string item, Action action) => ExecuteItem(item, action);

        public void RunItemFunc(string item, Func<KifaActionResult> action)
            => ExecuteItem(item, action);

        public KifaActionResult<(string Choice, int? Part, int Index, bool Special)> RunSelectOne(
            List<string> choices, string key)
            => SelectOne(choices, s => s, selectionKey: key);

        public KifaActionResult<List<string>> RunSelectMany(List<string> choices, string key)
            => SelectMany(choices, s => s, selectionKey: key);

        public bool RunConfirm(string prefix, bool suggested, string key)
            => Confirm(prefix, suggested, selectionKey: key);
    }

    [Fact]
    public void ExecuteItem_ExecutesWhenStopNotRequested() {
        var cmd = new TestCommand { StopRequested = false };

        var executed = false;
        cmd.RunItemAction("test1", () => {
            executed = true;
        });

        Assert.True(executed);
        Assert.Single(cmd.Results);
        Assert.Equal(KifaActionStatus.OK, cmd.Results[0].result.Status);
    }

    [Fact]
    public void ExecuteItem_CancelsWhenStopRequested() {
        var cmd = new TestCommand { StopRequested = true };
        var executed = false;
        cmd.RunItemAction("test2", () => {
            executed = true;
        });

        Assert.False(executed);
        Assert.Single(cmd.Results);
        Assert.Equal(KifaActionStatus.Cancelled, cmd.Results[0].result.Status);
        Assert.Contains("stop was requested", cmd.Results[0].result.Message);
    }

    [Fact]
    public void ExecuteItem_Func_CancelsWhenStopRequested() {
        var cmd = new TestCommand { StopRequested = true };
        var executed = false;
        cmd.RunItemFunc("test3", () => {
            executed = true;
            return KifaActionResult.Success();
        });

        Assert.False(executed);
        Assert.Single(cmd.Results);
        Assert.Equal(KifaActionStatus.Cancelled, cmd.Results[0].result.Status);
    }

    [Fact]
    public void SelectOne_AbortsWhenStopRequested() {
        var cmd = new TestCommand { StopRequested = true };
        var res = cmd.RunSelectOne(new List<string> { "a", "b" }, "stop_test_one");
        Assert.Equal(KifaActionStatus.Cancelled, res.Status);
    }

    [Fact]
    public void SelectMany_AbortsWhenStopRequested() {
        var cmd = new TestCommand { StopRequested = true };
        var res = cmd.RunSelectMany(new List<string> { "a", "b" }, "stop_test_many");
        Assert.Equal(KifaActionStatus.Cancelled, res.Status);
    }

    [Fact]
    public void Confirm_AbortsWhenStopRequested() {
        var cmd = new TestCommand { StopRequested = true };
        var res = cmd.RunConfirm("Continue?", true, "stop_test_confirm");
        Assert.False(res);
    }
}
