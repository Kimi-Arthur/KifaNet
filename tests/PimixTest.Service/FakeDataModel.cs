using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace PimixTest.Service {
    class FakeDataModel : DataModel {
        public const string ModelId = "api_test";

        static FakeDataModelServiceClient client;

        public static FakeDataModelServiceClient Client => client =
            client ?? new FakeDataModelRestServiceClient();

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

    interface FakeDataModelServiceClient : PimixServiceClient<FakeDataModel> {
        void Reset();
    }

    class FakeDataModelRestServiceClient : PimixServiceRestClient<FakeDataModel>, FakeDataModelServiceClient {
        public void Reset() => Call<FakeDataModel>("reset");
    }
}
