using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pimix.Service
{
    public static class PimixService
    {
        static Dictionary<Type, Tuple<PropertyInfo, string>> typeCache
            = new Dictionary<Type, Tuple<PropertyInfo, string>>();

        public static string PimixServerApiAddress { get; set; }

        public static string PimixServerCredential { get; set; }

        public static bool Patch<TDataModel>(TDataModel data, string id = null)
        {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            id = id ?? typeInfo.Item1.GetValue(data) as string;
            string content = JsonConvert.SerializeObject(data);
            string address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{id}";

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

        public static bool Post<TDataModel>(TDataModel data, string id = null)
        {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            id = id ?? typeInfo.Item1.GetValue(data) as string;
            string content = JsonConvert.SerializeObject(data);
            string address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{id}";

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = "POST";
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
        {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            string address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{id}";

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = "GET";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var response = request.GetResponse())
            {
                return response.GetObject<TDataModel>();
            }
        }

        public static bool Delete<TDataModel>(string id)
        {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            string address =
                $"{PimixServerApiAddress}/{typeInfo.Item2}/{id}";

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = "DELETE";
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            using (var response = request.GetResponse())
            {
                return response.GetDictionary()["status"].ToString() == "ok";
            }
        }

        public static ResponseType Call<TDataModel, ResponseType>(string action, string methodType = "GET",
            string id = null, Dictionary<string, string> parameters = null, Object body = null)
        {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            string address = $"{PimixServerApiAddress}/{typeInfo.Item2}{id?.Insert(0, "/")}/${action}";
            if (parameters != null)
            {
                address += "?" + string.Join("&", parameters.Select(item => $"{item.Key}={item.Value}"));
            }

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = methodType;
            request.Headers["Authorization"] =
                $"Basic {PimixServerCredential}";

            if (body != null)
            {
                using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
                {
                    string content = JsonConvert.SerializeObject(body);
                    sw.Write(content);
                }
            }

            using (var response = request.GetResponse())
            {
                return response.GetObject<ResponseType>();
            }
        }

        static void Init(Type typeInfo)
        {
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                };

            if (!typeCache.ContainsKey(typeInfo))
            {
                // TODO: Will throw. Can add custom exceptions.
                PropertyInfo idProp = typeInfo.GetProperty("Id");
                DataModelAttribute dmAttr = typeInfo.GetCustomAttribute<DataModelAttribute>();
                typeCache[typeInfo] = Tuple.Create(idProp, dmAttr.ModelId);
            }
        }
    }
}
