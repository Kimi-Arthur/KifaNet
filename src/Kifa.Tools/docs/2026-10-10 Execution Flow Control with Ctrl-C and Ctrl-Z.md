# Execution Flow Control with Ctrl-C and Ctrl-Z

## Overview
Long-running CLI commands in KifaNet (such as `filex cp`, `filex rm`, and `subx`) typically process sequences or batches of items sequentially. Standard terminal behavior for interrupt signals (`Ctrl-C` / `SIGINT`) and suspend signals (`Ctrl-Z` / `SIGTSTP`) can be destructive:
- Unhandled `SIGINT` abruptly kills the process mid-operation, potentially corrupting inflight transfers, leaking resources, and skipping final summary statistics.
- Traditional deferred cancellation forces users to wait for the entire current item or task to complete, with no option for immediate exit if an operation hangs.
- Unhandled `SIGTSTP` suspends the process without releasing interactive session locks or notifying the user of paused progress.

[`KifaCommand`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs) implements dual-signal flow control supporting graceful deferred cancellation, immediate force exit, graceful pause between items, immediate process suspension, and toggling of requested states using double-tap detection.

---

## Signal Handling Semantics

All control operations use a 500ms double-tap threshold (`DoublePressThresholdMs`):

### 1. `Ctrl-C` (Stop / Cancellation)

| Action | Timing / Condition | Behavior |
|---|---|---|
| **Single Press** | First press during normal run | **Graceful Stop**: Completes the currently executing item. Subsequent items in the batch are cancelled with `KifaActionStatus.Cancelled`. Logs a warning and prints cancellation notice. |
| **Double Press** | Second press within 500ms | **Immediate Force Exit**: Immediately interrupts execution. Marks the active item as cancelled, logs the final summary report with `LogSummary()`, invokes registered `KifaShutdown` cleanup handlers, and exits the process (`Environment.Exit(130)`). |
| **Subsequent Press** | Press after > 500ms while stop is pending | **Toggle / Resume**: Cancels the pending stop request and resumes normal execution of subsequent items. |
| **During Prompt** | Press while awaiting user input | **Cancel Prompt**: Aborts the current interactive prompt without forcing immediate process exit. |
| **While Paused** | Press while paused between items | **Cancel Remaining**: Unblocks the pause loop immediately and cancels remaining items. |

### 2. `Ctrl-Z` (Pause / Suspend)

| Action | Timing / Condition | Behavior |
|---|---|---|
| **Single Press** | First press during normal run | **Graceful Pause**: Completes the currently executing item, then enters a non-busy wait loop (`WaitIfPaused`) before starting the next item. Execution can be resumed by pressing `Enter` or `Ctrl-Z`. |
| **Double Press** | Second press within 500ms | **Immediate Suspend**: Bypasses deferred pause and immediately suspends the process at the OS kernel level via `SIGSTOP`. Shell job control takes over; typing `fg` in terminal resumes the process via `SIGCONT`. |
| **Subsequent Press** | Press after > 500ms while pause is pending | **Toggle / Resume**: Cancels the pending pause request and resumes execution immediately. |
| **When Stop Pending**| Press while a graceful stop was requested | **Switch to Pause**: Overrides the stop request with a graceful pause request instead. |

---

## Architecture & Implementation

### 1. Signal Interception
Terminal events and POSIX signals are captured during command execution in [`ExecuteCommand`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs#L258-L318):
- **`Console.CancelKeyPress`**: Intercepts `Ctrl-C`. Sets `e.Cancel = true` to prevent the .NET runtime from immediately terminating the process, routing control to [`HandleCancelKeyPress()`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs#L179-L211).
- **`PosixSignalRegistration` (Non-Windows)**:
  - `PosixSignal.SIGTSTP` (`Ctrl-Z`): Sets `ctx.Cancel = true` to prevent automatic process stop, routing control to [`HandleSuspendSignal()`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs#L213-L249).
  - `PosixSignal.SIGCONT` (Terminal `fg`): Listens for process resumption to reset state timers via [`HandleResumeSignal()`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs#L251-L256).

### 2. Immediate OS Suspension (`SIGSTOP`)
When an immediate suspend is triggered via double-tap `Ctrl-Z`, the process sends `SIGSTOP` to its own PID via standard POSIX `libc` `kill(2)`:
- macOS (Darwin): `SIGSTOP = 17`
- Linux: `SIGSTOP = 19`

Because `SIGSTOP` cannot be caught or ignored by user processes, the operating system kernel immediately freezes process execution until `SIGCONT` is received from the shell.

### 3. Graceful Pause Loop (`WaitIfPaused`)
Between items, [`ExecuteItem`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.Execution.cs#L20-L106) invokes [`WaitIfPaused(item)`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs#L89-L121). If `PauseRequested` is set:
- Logs warning prompting user: `Execution paused before '{item}'. Press Enter or Ctrl-Z to resume, or Ctrl-C to stop...`
- Waits efficiently using an `AutoResetEvent` (`pauseSignal`) with a 50ms periodic check for terminal key presses (`Console.KeyAvailable` / `Console.ReadKey`).
- Pressing `Enter` or sending another `Ctrl-Z` clears `PauseRequested` and pulses `pauseSignal`, resuming execution cleanly.

### 4. Thread-Safe Result Tracking & Summary
Because signals are received asynchronously on CLR thread-pool threads:
- Access to `Results` is synchronized with `lock (Results)`.
- The currently executing item is tracked in [`CurrentItem`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs#L79-L82).
- When a forced exit occurs, [`DefaultImmediateCancel()`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.cs#L151-L171) records `CurrentItem` as `KifaActionStatus.Cancelled` ("Interrupted by user forced exit"), prints the final [`LogSummary()`](file:///Users/kimi/Projects/KifaNet/src/Kifa.Tools/KifaCommand.Execution.cs#L108-L203), runs registered shutdown hooks via `KifaShutdown.RunCleanups()`, and terminates cleanly.

### 5. Inversion of Control & Testability
To allow thorough unit testing without killing the test runner or suspending test processes:
- `ImmediateCancelAction` defaults to `DefaultImmediateCancel` (`Environment.Exit(130)`), but can be replaced by mock delegates in unit tests.
- `SuspendAction` defaults to `DefaultSuspend` (`kill(pid, SIGSTOP)`), but can be replaced in unit tests.
- `IsKeyAvailableFunc` and `ReadKeyFunc` can be injected to simulate interactive terminal key inputs.

---

## Unit Testing

Comprehensive test cases in [`KifaCommandStopTests`](file:///Users/kimi/Projects/KifaNet/Tests/Kifa.Tools.Tests/KifaCommandStopTests.cs) verify:
- Graceful cancellation (`Ctrl-C` single press marks remaining items cancelled after current item finishes).
- Double-tap `Ctrl-C` within 500ms triggers immediate forced exit and cancellation of the running item.
- Subsequent `Ctrl-C` after 500ms toggles cancellation back to active execution.
- Graceful pause (`Ctrl-Z` single press pauses execution before next item and resumes on Enter or subsequent `Ctrl-Z`).
- Double-tap `Ctrl-Z` within 500ms triggers `SuspendAction`.
- Transitioning from stop to pause when `Ctrl-Z` is pressed after `Ctrl-C`.
- Cancelling while paused immediately unblocks pause loop and cancels remaining items.
