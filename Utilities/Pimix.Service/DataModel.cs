using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pimix.Service
{
    public abstract class DataModel
    {
        public DataModel()
        {
            // Do not override, use property setting instead
            // or create other ctors.
            // This ctor does nothing. Used only for accessing ModelId.
        }

        [JsonIgnore]
        public virtual string ModelId
            => null;

        // Use two id properties to handle special '$id' in json.net
        [JsonProperty("$id")]
        public string Id { get; set; }

        [JsonProperty]
        private string _id
        {
            get
            {
                return null;
            }
            set
            {
                Id = value;
            }
        }

        public static string PimixServerApiAddress { get; set; }

        public static string PimixServerCredential { get; set; }

        public static void Init()
        {
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
        }

        public static bool Patch<TDataModel>(TDataModel data, string id = null)
            where TDataModel : DataModel
        {
            Init();
            id = id ?? data.Id;
            string content = JsonConvert.SerializeObject(data);
            string address =
                $"{PimixServerApiAddress}/{data.ModelId}/{id}";

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = "PATCH";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
            {
                sw.Write(content);
            }

            using (var response = request.GetResponse())
            {
                return response.GetDictionary()["status"].ToString() == "ok";
            }
        }

        public static TDataModel Get<TDataModel>(string id)
            where TDataModel : DataModel, new()
        {
            Init();

            // Suppose new a TDataModel is cheap.
            string address =
                $"{PimixServerApiAddress}/{new TDataModel().ModelId}/{id}?no$";

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = "GET";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var response = request.GetResponse())
            {
                return response.GetObject<TDataModel>();
            }
        }
    }
}
