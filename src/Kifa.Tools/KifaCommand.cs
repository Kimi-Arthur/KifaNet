using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Kifa.Api.Files;
using Kifa.Configs;
using Kifa.Jobs;
using Kifa.Service;
using NLog;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static KifaFile CurrentFolder => new(".");

    [Option('v', "verbose", HelpText = "Show most detailed log.")]
    public bool Verbose { get; set; } = false;

    [Option('V', "non-verbose", HelpText = "Show least detailed log.")]
    public bool NonVerbose { get; set; } = false;

    [Option("config", HelpText = "Use an alternative config file.")]
    public string? ConfigFile { get; set; }

    public static int Run(Func<string[], ParserResult<object>> parse, string[] args) {
        return parse(args).MapResult<KifaCommand, int>(ExecuteCommand, HandleParseFail);
    }

    public static int Run(string[] args, params Type[] types)
        => Run(
            parameters => new Parser(settings => {
                settings.CaseInsensitiveEnumValues = true;
                settings.HelpWriter = Console.Error;
                settings.EnableDashDash = true;
            }).ParseArguments(parameters, types), args);

    volatile bool pauseRequested;

    public bool PauseRequested {
        get => pauseRequested;
        set {
            pauseRequested = value;
            if (!value) {
                pauseSignal.Set();
            }
        }
    }

    volatile bool stopRequested;

    public bool StopRequested {
        get => stopRequested;
        set {
            stopRequested = value;
            if (value) {
                pauseSignal.Set();
            }
        }
    }

    volatile bool isPrompting;

    public bool IsPrompting {
        get => isPrompting;
        set => isPrompting = value;
    }

    volatile bool isPaused;

    public bool IsPaused => isPaused;

    volatile string? currentItem;

    public string? CurrentItem {
        get => currentItem;
        set => currentItem = value;
    }

    readonly AutoResetEvent pauseSignal = new(false);

    public Func<bool>? IsKeyAvailableFunc { get; set; }
    public Func<ConsoleKeyInfo>? ReadKeyFunc { get; set; }

    public void WaitIfPaused(string item) {
        if (!PauseRequested || StopRequested) {
            return;
        }

        Logger.Warn(
            $"Execution paused before '{item}'. Press Enter or Ctrl-Z to resume, or Ctrl-C to stop...");

        var keyAvailableFunc = IsKeyAvailableFunc ?? (() => !Console.IsInputRedirected && Console.KeyAvailable);
        var readKeyFunc = ReadKeyFunc ?? (() => Console.ReadKey(intercept: true));

        isPaused = true;
        try {
            while (PauseRequested && !StopRequested) {
                try {
                    if (keyAvailableFunc()) {
                        var key = readKeyFunc();
                        if (key.Key == ConsoleKey.Enter) {
                            PauseRequested = false;
                            Logger.Info("Resuming execution...");
                            break;
                        }
                    }
                } catch {
                    // Ignore errors checking or reading keys.
                }

                pauseSignal.WaitOne(50);
            }
        } finally {
            isPaused = false;
        }
    }

    readonly object stateLock = new();
    DateTimeOffset lastCtrlCTime = DateTimeOffset.MinValue;
    DateTimeOffset lastCtrlZTime = DateTimeOffset.MinValue;

    Action? immediateCancelAction;

    public Action ImmediateCancelAction {
        get => immediateCancelAction ?? DefaultImmediateCancel;
        set => immediateCancelAction = value;
    }

    Action? suspendAction;

    public Action SuspendAction {
        get => suspendAction ?? DefaultSuspend;
        set => suspendAction = value;
    }

    const int DoublePressThresholdMs = 500;
    const int PosixSigStopMacOs = 17;
    const int PosixSigStopLinux = 19;

    [DllImport("libc", SetLastError = true)]
    static extern int kill(int pid, int sig);

    static readonly int SigStop =
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PosixSigStopMacOs : PosixSigStopLinux;

    void DefaultImmediateCancel() {
        Logger.Warn("Forced exit requested. Cleaning up...");
        try {
            lock (Results) {
                var runningItem = currentItem;
                if (runningItem != null && !Results.Any(r => r.item == runningItem)) {
                    Results.Add((runningItem, new KifaActionResult {
                        Status = KifaActionStatus.Cancelled,
                        Message = "Interrupted by user forced exit."
                    }));
                }
            }

            LogSummary();
        } catch (Exception ex) {
            Logger.Error(ex, "Failed to log summary during forced exit.");
        }

        KifaShutdown.RunCleanups();
        Environment.Exit(130);
    }

    void DefaultSuspend() {
        if (!OperatingSystem.IsWindows()) {
            kill(Environment.ProcessId, SigStop);
        }
    }

    public void HandleCancelKeyPress(DateTimeOffset? pressTime = null) {
        var now = pressTime ?? DateTimeOffset.UtcNow;
        lock (stateLock) {
            if (now - lastCtrlCTime <= TimeSpan.FromMilliseconds(DoublePressThresholdMs)) {
                ImmediateCancelAction();
                return;
            }

            lastCtrlCTime = now;

            if (IsPrompting) {
                StopRequested = true;
                Logger.Warn(
                    "Stop requested. Cancelling prompt... (Double-press Ctrl-C to cancel immediately)");
                return;
            }

            if (StopRequested) {
                StopRequested = false;
                Logger.Info("Stop request cancelled. Resuming normal execution.");
            } else {
                StopRequested = true;
                PauseRequested = false;
                if (IsPaused) {
                    Logger.Warn(
                        "Stop requested. Cancelling remaining items... (Press Ctrl-C again to resume, double-press to cancel immediately)");
                } else {
                    Logger.Warn(
                        "Stop requested. Completing current item before stopping... (Press Ctrl-C again to resume, double-press to cancel immediately)");
                }
            }
        }
    }

    public void HandleSuspendSignal(DateTimeOffset? pressTime = null) {
        var now = pressTime ?? DateTimeOffset.UtcNow;
        lock (stateLock) {
            if (now - lastCtrlZTime <= TimeSpan.FromMilliseconds(DoublePressThresholdMs)) {
                Logger.Warn("Immediate pause requested. Suspending process... (Use 'fg' to resume)");
                if (!isPaused) {
                    PauseRequested = false;
                }

                SuspendAction();
                return;
            }

            lastCtrlZTime = now;

            if (StopRequested) {
                StopRequested = false;
                PauseRequested = true;
                Logger.Warn(
                    "Stop request replaced with pause. Completing current item before pausing... (Press Ctrl-Z to resume, double-press to pause now)");
                return;
            }

            if (PauseRequested) {
                PauseRequested = false;
                if (IsPaused) {
                    Logger.Info("Resuming execution...");
                } else {
                    Logger.Info("Pause request cancelled. Resuming normal execution.");
                }
            } else {
                PauseRequested = true;
                Logger.Warn(
                    "Pause requested. Completing current item before pausing... (Press Ctrl-Z to resume, double-press to pause now)");
            }
        }
    }

    public void HandleResumeSignal() {
        lock (stateLock) {
            lastCtrlZTime = DateTimeOffset.MinValue;
            Logger.Info("Resumed execution.");
        }
    }

    static int ExecuteCommand(KifaCommand command) {
        ResetInteractionState();
        command.PauseRequested = false;
        command.StopRequested = false;
        command.IsPrompting = false;
        command.pauseSignal.Reset();
        lock (command.stateLock) {
            command.lastCtrlCTime = DateTimeOffset.MinValue;
            command.lastCtrlZTime = DateTimeOffset.MinValue;
        }

        ConsoleCancelEventHandler cancelHandler = (sender, e) => {
            e.Cancel = true;
            command.HandleCancelKeyPress();
        };

        Console.CancelKeyPress += cancelHandler;

        PosixSignalRegistration? tstpRegistration = null;
        PosixSignalRegistration? contRegistration = null;
        if (!OperatingSystem.IsWindows()) {
            try {
                tstpRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTSTP, ctx => {
                    ctx.Cancel = true;
                    command.HandleSuspendSignal();
                });
                contRegistration = PosixSignalRegistration.Create(PosixSignal.SIGCONT, ctx => {
                    command.HandleResumeSignal();
                });
            } catch (Exception ex) {
                Logger.Warn(ex, "Failed to register POSIX signal handlers.");
            }
        }

        KifaConfigs.Init(command.ConfigFile);

        if (command.Verbose) {
            global::Kifa.Logging.EnableNotice = true;
            Logging.ConfigureLogger(true);
        } else if (command.NonVerbose) {
            Logging.ConfigureLogger(false);
        } else {
            Logging.ConfigureLogger();
        }

        try {
            return command.Execute();
        } catch (Exception ex) {
            while (ex != null) {
                Console.WriteLine("Caused by:");
                Console.WriteLine(ex);
                ex = ex.InnerException;
            }

            return 1;
        } finally {
            Console.CancelKeyPress -= cancelHandler;
            tstpRegistration?.Dispose();
            contRegistration?.Dispose();
        }
    }

    static int HandleParseFail(IEnumerable<Error> errors) => 2;

    public abstract int Execute(KifaTask? task = null);
}
