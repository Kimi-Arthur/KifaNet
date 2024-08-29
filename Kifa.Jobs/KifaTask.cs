using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Jobs;

public class KifaTask {
    public string? Name { get; set; }

    public List<KifaTask> CompletedSubTasks { get; set; } = [];

    public KifaTask? CurrentTask { get; set; }

    public List<KifaTask> NextSubTasks { get; set; } = [];

    [JsonIgnore]
    [YamlIgnore]
    public bool IsSingleTask
        => CurrentTask == null && CompletedSubTasks.Count == 0 && NextSubTasks.Count == 0;

    public int TotalProgress { get; set; }
    public int CompletedProgress { get; set; }
}
