using System;
using System.Collections.Generic;
using Kifa.Jobs;

namespace Kifa.Tools;

public abstract partial class KifaCommand {
    public string? RunId { get; set; }

    // Indexes of current task ids in each level.
    public List<string> CurrentTaskIds { get; set; } = [];

    protected void CreateRun(List<string> args) {
        RunId = "generate a sortable run id";
        KifaRun.Client.Set(new KifaRun {
            Arguments = args
        });
        throw new NotImplementedException();
    }

    protected void StartJob() {
        throw new NotImplementedException();
    }

    protected void AddSubTasks() {
        throw new NotImplementedException();
    }
}
