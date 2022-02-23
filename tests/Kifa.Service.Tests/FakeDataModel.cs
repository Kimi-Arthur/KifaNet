using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kifa.Service.Tests {
    class FakeDataModel : DataModel<FakeDataModel> {
        public const string ModelId = "api_test";

        static FakeDataModelServiceClient client;

        public static FakeDataModelServiceClient Client =>
            client ??= new FakeDataModelRestServiceClient();

        public int? IntPROP { get; set; }

        public string StrProp { get; set; }

        public List<string> ListProp { get; set; }

        public Dictionary<string, string> DictProp { get; set; }

        public FakeSubDataModel SubProp { get; set; }
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
}
