using System;
using System.Configuration;
using System.Text;
using CommandLine;
using Pimix.Service;

namespace Pimix.Apps.JobUtil {
    abstract class JobUtilCommand {
        public static string ClientName { get; set; }

        public static TimeSpan HeartbeatInterval { get; set; }

        [Option('b', "fire-heartbeat", HelpText =
            "Whether to fire heartbeat during job execution.")]
        public bool FireHeartbeat { get; set; } = false;

        public abstract int Execute();
    }
}
