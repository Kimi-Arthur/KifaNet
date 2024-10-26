using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Jobs;

// Represents one run of a KifaCommand.
public class KifaRun : DataModel, WithModelId<KifaRun> {
    public static string ModelId => "jobs/runs";

    public static KifaServiceClient<KifaRun> Client { get; set; } =
        new KifaServiceRestClient<KifaRun>();

    // Command
    public string? Tool { get; set; }
    public string? Version { get; set; }
    public List<string> Arguments { get; set; } = [];

    // Environment
    public string? Host { get; set; }
    public string? ProcessId { get; set; }
    public DateTimeOffset StartTime { get; set; }

    // Logging? Not used for now.
    public string? LogFilePath { get; set; }

    // Progress. Maybe we should make this class a KifaTask by itself.
    public Link<KifaTask> Task { get; set; } = new() {Id = "abc"};

    public IEnumerable<KifaTask> CurrentTasks {
        get {
            var currentTask = Task.Data.Checked();
            yield return currentTask;
            while (currentTask.CurrentTask != null) {
                currentTask = currentTask.CurrentTask.Data.Checked();
                yield return currentTask;
            }
        }
    }
}
