using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;

namespace Kifa.Service;

public interface KifaServiceClient<TDataModel> where TDataModel : DataModel {
    string ModelId { get; }

    SortedDictionary<string, TDataModel> List();
    TDataModel? Get(string id, bool refresh = false);
    List<TDataModel?> Get(List<string> ids);
    KifaActionResult Set(TDataModel data);
    KifaActionResult Set(List<TDataModel> data);
    KifaActionResult Update(TDataModel data);
    KifaActionResult Update(List<TDataModel> data);
    KifaActionResult Delete(string id);
    KifaActionResult Delete(List<string> ids);
    KifaActionResult Link(string targetId, string linkId);
}

public abstract class BaseKifaServiceClient<TDataModel> : KifaServiceClient<TDataModel>
    where TDataModel : DataModel {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected BaseKifaServiceClient() {
        var typeInfo = typeof(TDataModel);
        ModelId = (string) typeInfo.GetField("ModelId")?.GetValue(null)!;
    }

    public string ModelId { get; }

    public abstract SortedDictionary<string, TDataModel> List();
    public abstract TDataModel? Get(string id, bool refresh = false);

    public virtual List<TDataModel?> Get(List<string> ids)
        => ids.AsParallel().Select(id => Get(id)).ToList();

    public abstract KifaActionResult Set(TDataModel data);

    public virtual KifaActionResult Set(List<TDataModel> data)
        => data.AsParallel().Select(Set).Aggregate(new KifaBatchActionResult(),
            (result, actionResult) => result.Add(actionResult));

    public abstract KifaActionResult Update(TDataModel data);

    public virtual KifaActionResult Update(List<TDataModel> data)
        => data.AsParallel().Select(Update).Aggregate(new KifaBatchActionResult(),
            (result, actionResult) => result.Add(actionResult));

    public abstract KifaActionResult Delete(string id);

    public virtual KifaActionResult Delete(List<string> ids)
        => ids.AsParallel().Select(Delete).Aggregate(new KifaBatchActionResult(),
            (result, actionResult) => result.Add(actionResult));

    public abstract KifaActionResult Link(string targetId, string linkId);
}
