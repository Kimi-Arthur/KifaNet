using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;

namespace PimixTest.Service
{
    class FakeDataModel : DataModel
    {
        public override string ModelId
            => "api_test";

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

        public static bool Reset()
        {
            var result = DataModel.Call<FakeDataModel, Dictionary<string, string>>("reset", methodType: "POST");
            return result.Contains(new KeyValuePair<string, string>("status", "ok"));
        }
    }

    class FakeSubDataModel
    {
        [JsonProperty("sub_prop1")]
        public string SubProp1 { get; set; }

        [JsonProperty("sub_prop2")]
        public List<string> SubProp2 { get; set; }
    }
}
