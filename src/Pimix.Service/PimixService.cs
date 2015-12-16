using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;

namespace Pimix.Service
{
    public static class PimixService
    {
        static Dictionary<Type, Tuple<PropertyInfo, string>> typeCache
            = new Dictionary<Type, Tuple<PropertyInfo, string>>();

        public static string PimixServerApiAddress { get; set; }

        public static string PimixServerCredential { get; set; }

        public static RetryPolicy DefaultRetryPolicy { get; set; }

        public static bool Patch<TDataModel>(TDataModel data, string id = null, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(() => PatchWithoutRetry<TDataModel>(data, id));

        public static bool PatchWithoutRetry<TDataModel>(TDataModel data, string id = null)
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
                return response.GetObject<ActionStatus>().StatusCode == ActionStatusCode.OK;
            }
        }

        public static bool Post<TDataModel>(TDataModel data, string id = null, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(() => PostWithoutRetry<TDataModel>(data, id));

        public static bool PostWithoutRetry<TDataModel>(TDataModel data, string id = null)
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
                return response.GetObject<ActionStatus>().StatusCode == ActionStatusCode.OK;
            }
        }

        public static TDataModel Get<TDataModel>(string id, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(() => GetWithoutRetry<TDataModel>(id));

        public static TDataModel GetWithoutRetry<TDataModel>(string id)
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

        public static bool Delete<TDataModel>(string id, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(() => DeleteWithoutRetry<TDataModel>(id));

        public static bool DeleteWithoutRetry<TDataModel>(string id)
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
                return response.GetObject<ActionStatus>().StatusCode == ActionStatusCode.OK;
            }
        }

        public static ResponseType Call<TDataModel, ResponseType>(string action, string methodType = "GET",
            string id = null, Dictionary<string, string> parameters = null, Object body = null, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(()
                => CallWithoutRetry<TDataModel, ResponseType>(action, methodType, id, parameters, body));

        public static ResponseType CallWithoutRetry<TDataModel, ResponseType>(string action, string methodType = "GET",
            string id = null, Dictionary<string, string> parameters = null, Object body = null)
        {
            Init(typeof(TDataModel));
            var typeInfo = typeCache[typeof(TDataModel)];

            string address = $"{PimixServerApiAddress}/{typeInfo.Item2}{id?.Insert(0, "/")}/${action}";
            if (parameters != null)
            {
                address += "?" + string.Join("&", parameters.Where(item => item.Value != null).Select(item => $"{item.Key}={item.Value}"));
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
                var result = response.GetObject<ActionStatus<ResponseType>>();
                if (result.StatusCode == ActionStatusCode.OK)
                {
                    return result.Message;
                }
                else
                {
                    throw new ActionFailedException { Response = result };
                }
            }
        }

        public static void Call<TDataModel>(string action, string methodType = "GET",
            string id = null, Dictionary<string, string> parameters = null, Object body = null, RetryPolicy retryPolicy = null)
            => (retryPolicy ?? DefaultRetryPolicy).ExecuteAction(()
                => CallWithoutRetry<TDataModel>(action, methodType, id, parameters, body));

        public static void CallWithoutRetry<TDataModel>(string action, string methodType = "GET",
            string id = null, Dictionary<string, string> parameters = null, Object body = null)
            => CallWithoutRetry<TDataModel, object>(action, methodType, id, parameters, body);

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

            if (DefaultRetryPolicy == null)
            {
                DefaultRetryPolicy = new RetryPolicy<PimixServiceTransientErrorDetectionStrategy>(5, TimeSpan.FromSeconds(10));
                DefaultRetryPolicy.Retrying += (sender, args) =>
                {
                    Console.Error.WriteLine("Pimix service call failed once, wait 10 seconds now:");
                    Console.Error.WriteLine(args.LastException);
                };
            }
        }
    }
}
