using System.Collections.Generic;
using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Jobs;

public class KifaTask : DataModel, WithModelId<KifaTask> {
    public static string ModelId => "jobs/tasks";

    public static KifaServiceClient<KifaTask> Client { get; set; } =
        new KifaServiceRestClient<KifaTask>();

    public string? Name { get; set; }

    public List<KifaTask> CompletedSubTasks { get; set; } = [];

    public Link<KifaTask>? CurrentTask { get; set; }

    public List<KifaTask> NextSubTasks { get; set; } = [];

    [JsonIgnore]
    [YamlIgnore]
    public bool IsSingleTask
        => CurrentTask == null && CompletedSubTasks.Count == 0 && NextSubTasks.Count == 0;

    public int TotalProgress { get; set; }
    public int CompletedProgress { get; set; }
}
