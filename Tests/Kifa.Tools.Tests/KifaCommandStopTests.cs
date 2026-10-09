using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    [Fact]
    public void WaitIfPaused_ReturnsImmediately_WhenPauseNotRequested() {
        var cmd = new TestCommand { PauseRequested = false };
        cmd.WaitIfPaused("test");
        Assert.False(cmd.IsPaused);
    }

    [Fact]
    public void WaitIfPaused_ReturnsImmediately_WhenStopRequested() {
        var cmd = new TestCommand { PauseRequested = true, StopRequested = true };
        cmd.WaitIfPaused("test");
        Assert.False(cmd.IsPaused);
    }

    [Fact]
    public void ExecuteItem_PausesAndResumes_WhenEnterPressed() {
        var cmd = new TestCommand {
            PauseRequested = true,
            IsKeyAvailableFunc = () => true,
            ReadKeyFunc = () => new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)
        };

        var executed = false;
        cmd.RunItemAction("test_resume", () => {
            executed = true;
        });

        Assert.True(executed);
        Assert.False(cmd.PauseRequested);
        Assert.Single(cmd.Results);
        Assert.Equal(KifaActionStatus.OK, cmd.Results[0].result.Status);
    }

    [Fact]
    public void ExecuteItem_PausesAndCancels_WhenStopRequestedWhilePaused() {
        var cmd = new TestCommand {
            PauseRequested = true,
            IsKeyAvailableFunc = () => false
        };

        _ = Task.Run(async () => {
            while (!cmd.IsPaused) {
                await Task.Delay(10);
            }

            cmd.StopRequested = true;
        });

        var executed = false;
        cmd.RunItemAction("test_pause_stop", () => {
            executed = true;
        });

        Assert.False(executed);
        Assert.True(cmd.StopRequested);
        Assert.Single(cmd.Results);
        Assert.Equal(KifaActionStatus.Cancelled, cmd.Results[0].result.Status);
    }

    [Fact]
    public void ExecuteItem_PausesAndResumes_WhenPauseRequestedSetToFalse() {
        var cmd = new TestCommand {
            PauseRequested = true,
            IsKeyAvailableFunc = () => false
        };

        _ = Task.Run(async () => {
            while (!cmd.IsPaused) {
                await Task.Delay(10);
            }

            cmd.PauseRequested = false;
        });

        var executed = false;
        cmd.RunItemAction("test_unpause", () => {
            executed = true;
        });

        Assert.True(executed);
        Assert.False(cmd.PauseRequested);
        Assert.Single(cmd.Results);
        Assert.Equal(KifaActionStatus.OK, cmd.Results[0].result.Status);
    }

    [Fact]
    public void HandleCancelKeyPress_SinglePress_SetsStopRequested() {
        var cmd = new TestCommand();
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleCancelKeyPress(t0);

        Assert.True(cmd.StopRequested);
        Assert.False(cmd.PauseRequested);
    }

    [Fact]
    public void HandleCancelKeyPress_DoublePressWithin500Ms_TriggersImmediateCancel() {
        var cancelCalled = false;
        var cmd = new TestCommand {
            ImmediateCancelAction = () => cancelCalled = true
        };
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleCancelKeyPress(t0);
        cmd.HandleCancelKeyPress(t0.AddMilliseconds(200));

        Assert.True(cancelCalled);
    }

    [Fact]
    public void HandleCancelKeyPress_LaterPressAfter500Ms_CancelsStopRequest() {
        var cmd = new TestCommand();
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleCancelKeyPress(t0);
        Assert.True(cmd.StopRequested);

        cmd.HandleCancelKeyPress(t0.AddMilliseconds(600));
        Assert.False(cmd.StopRequested);
    }

    [Fact]
    public void HandleCancelKeyPress_DoublePressAfterLaterPress_TriggersImmediateCancel() {
        var cancelCalled = false;
        var cmd = new TestCommand {
            ImmediateCancelAction = () => cancelCalled = true
        };
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleCancelKeyPress(t0);
        Assert.True(cmd.StopRequested);

        // Later press cancels stop request
        cmd.HandleCancelKeyPress(t0.AddMilliseconds(1000));
        Assert.False(cmd.StopRequested);

        // Quick second press within 500ms triggers immediate cancel
        cmd.HandleCancelKeyPress(t0.AddMilliseconds(1200));
        Assert.True(cancelCalled);
    }

    [Fact]
    public void HandleCancelKeyPress_WhenIsPrompting_SetsStopRequested() {
        var cmd = new TestCommand { IsPrompting = true };
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleCancelKeyPress(t0);

        Assert.True(cmd.StopRequested);
    }

    [Fact]
    public void HandleSuspendSignal_SinglePress_SetsPauseRequested() {
        var cmd = new TestCommand();
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleSuspendSignal(t0);

        Assert.True(cmd.PauseRequested);
        Assert.False(cmd.StopRequested);
    }

    [Fact]
    public void HandleSuspendSignal_DoublePressWithin500Ms_TriggersImmediateSuspend() {
        var suspendCalled = false;
        var cmd = new TestCommand {
            SuspendAction = () => suspendCalled = true
        };
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleSuspendSignal(t0);
        cmd.HandleSuspendSignal(t0.AddMilliseconds(300));

        Assert.True(suspendCalled);
        Assert.False(cmd.PauseRequested);
    }

    [Fact]
    public void HandleSuspendSignal_LaterPressAfter500Ms_CancelsPauseRequest() {
        var cmd = new TestCommand();
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleSuspendSignal(t0);
        Assert.True(cmd.PauseRequested);

        cmd.HandleSuspendSignal(t0.AddMilliseconds(600));
        Assert.False(cmd.PauseRequested);
    }

    [Fact]
    public void HandleSuspendSignal_WhenStopRequested_SwitchesToPause() {
        var cmd = new TestCommand { StopRequested = true };
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleSuspendSignal(t0);

        Assert.False(cmd.StopRequested);
        Assert.True(cmd.PauseRequested);
    }

    [Fact]
    public void HandleCancelKeyPress_WhenPauseRequested_SwitchesToStop() {
        var cmd = new TestCommand { PauseRequested = true };
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleCancelKeyPress(t0);

        Assert.True(cmd.StopRequested);
        Assert.False(cmd.PauseRequested);
    }

    [Fact]
    public void HandleResumeSignal_ResetsLastCtrlZTime() {
        var suspendCalled = false;
        var cmd = new TestCommand {
            SuspendAction = () => suspendCalled = true
        };
        var t0 = DateTimeOffset.UtcNow;

        cmd.HandleSuspendSignal(t0);
        cmd.HandleSuspendSignal(t0.AddMilliseconds(200));
        Assert.True(suspendCalled);

        suspendCalled = false;
        cmd.HandleResumeSignal();

        // After resume, press at 300ms is not a double-press of the one before resume
        cmd.HandleSuspendSignal(t0.AddMilliseconds(300));
        Assert.False(suspendCalled);
        Assert.True(cmd.PauseRequested);
    }
}
