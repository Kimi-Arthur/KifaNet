using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;

namespace PimixTest.Service {
    [DataModel("api_test")]
    class FakeDataModel {
        public string Id { get; set; }

        public int? IntPROP { get; set; }

        public string StrProp { get; set; }

        public List<string> ListProp { get; set; }

        public Dictionary<string, string> DictProp { get; set; }

        public FakeSubDataModel SubProp { get; set; }

        #region PimixService Wrappers

        public static void Patch(FakeDataModel data, string id = null)
            => PimixService.Update(data, id);

        public static FakeDataModel Get(string id) => PimixService.Get<FakeDataModel>(id);

        #endregion

        public static void Reset() => PimixService.Call<FakeDataModel>("reset");
    }

    class FakeSubDataModel {
        public string SubProp1 { get; set; }

        [JsonProperty("sub_prop2")]
        public List<string> Sub2 { get; set; }
    }
}
