using System;
using System.Collections.Generic;
using Kifa.Jobs;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    public string? RunId { get; set; }

    protected void CreateRun(List<string> args) {
        RunId = "generate a sortable run id";
        var run = new KifaRun {
            Tool = null,
            Version = null,
            Arguments = args,
            Host = null,
            ProcessId = null,
            StartTime = DateTimeOffset.UtcNow
        };
        KifaRun.Client.Set(run);
        throw new NotImplementedException();
    }
}
