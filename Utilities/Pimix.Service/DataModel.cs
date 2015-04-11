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
        public virtual string ModelId
            => "invalid";

        public virtual string Id { get; set; }

        public static string PimixServerApiAddress { get; set; }

        public static string PimixServerCredential { get; set; }

        public static void Init()
        {
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        }

        public static bool Patch<TDataModel>(TDataModel data, string id = null)
            where TDataModel : DataModel
        {
            Init();
            id = id ?? data.Id;
            string content = JsonConvert.SerializeObject(data);
            string address =
                $"{PimixServerApiAddress}/{data.ModelId}/{Uri.EscapeDataString(Uri.EscapeDataString(id))}";
            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = "PATCH";
            request.Headers["Authorization"] = $"Basic {PimixServerCredential}";

            using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
            {
                sw.Write(content);
            }

            using (var response = request.GetResponse())
            {
                return response.GetDictionary()["status"].ToString() == "ok";
            }
        }
    }
}
