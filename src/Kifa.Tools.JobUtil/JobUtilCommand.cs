using System;
using CommandLine;

namespace Kifa.Tools.JobUtil; 

abstract class JobUtilCommand {
    public static string ClientName { get; set; }

    public static TimeSpan HeartbeatInterval { get; set; }

    [Option('b', "fire-heartbeat", HelpText =
        "Whether to fire heartbeat during job execution.")]
    public bool FireHeartbeat { get; set; } = false;

    public abstract int Execute();
}