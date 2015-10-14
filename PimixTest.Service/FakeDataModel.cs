using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;

namespace PimixTest.Service
{
    [DataModel("api_test")]
    class FakeDataModel
    {
        [JsonProperty("$id")]
        public string Id { get; set; }

        [JsonProperty("int_prop")]
        public int? IntProp { get; set; }

        [JsonProperty("str_prop")]
        public string StrProp { get; set; }

        [JsonProperty("list_prop")]
        public List<string> ListProp { get; set; }

        [JsonProperty("dict_prop")]
        public Dictionary<string, string> DictProp { get; set; }

        [JsonProperty("sub_prop")]
        public FakeSubDataModel SubProp { get; set; }

        #region PimixService Wrappers

        public static string PimixServerApiAddress
        {
            get
            {
                return PimixService.PimixServerApiAddress;
            }
            set
            {
                PimixService.PimixServerApiAddress = value;
            }
        }

        public static string PimixServerCredential
        {
            get
            {
                return PimixService.PimixServerCredential;
            }
            set
            {
                PimixService.PimixServerCredential = value;
            }
        }

        public static bool Patch(FakeDataModel data, string id = null)
            => PimixService.Patch<FakeDataModel>(data, id);

        public static FakeDataModel Get(string id)
            => PimixService.Get<FakeDataModel>(id);

        #endregion

        public static void Reset()
            => PimixService.Call<FakeDataModel>("reset", methodType: "POST");
    }

    class FakeSubDataModel
    {
        [JsonProperty("sub_prop1")]
        public string SubProp1 { get; set; }

        [JsonProperty("sub_prop2")]
        public List<string> SubProp2 { get; set; }
    }
}
