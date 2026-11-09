using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kifa.Service.Tests;

class FakeDataModel : DataModel, WithModelId<FakeDataModel> {
    public static string ModelId => "api_test";

    public static Language? DefaultLanguage => Language.English;

    static FakeDataModelServiceClient client;

    public static FakeDataModelServiceClient Client
        => client ??= new FakeDataModelRestServiceClient();

    public int? IntPROP { get; set; }

    public string StrProp { get; set; }

    public List<string> ListProp { get; set; }

    public Dictionary<string, string> DictProp { get; set; }

    public FakeSubDataModel SubProp { get; set; }

    public List<FakeKeyedItem>? KeyedItems { get; set; }

    public List<FakePositionalItem>? PositionalItems { get; set; }
}

class FakeKeyedItem {
    public string? Id { get; set; }
    public string? Meaning { get; set; }
    public string? Cefr { get; set; }
    public List<FakePositionalItem>? Examples { get; set; }
}

class FakePositionalItem {
    public string? Text { get; set; }
    public string? Translation { get; set; }
}

class FakeSubDataModel {
    public string SubProp1 { get; set; }

    [JsonProperty("sub_prop2")]
    public List<string> Sub2 { get; set; }
}

interface FakeDataModelServiceClient : KifaServiceClient<FakeDataModel> {
    void Reset();
}

class FakeDataModelRestServiceClient : KifaServiceRestClient<FakeDataModel>,
    FakeDataModelServiceClient {
    public void Reset() => Call<FakeDataModel>("reset");
}
