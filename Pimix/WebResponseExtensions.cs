using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pimix
{
    public static class WebResponseExtensions
    {
        public static T GetObject<T>(this WebResponse response)
        {
            var resp = response as HttpWebResponse;
            string encodingName = resp.ContentEncoding;
            if (string.IsNullOrEmpty(encodingName))
                encodingName = resp.CharacterSet;
            if (string.IsNullOrEmpty(encodingName))
                encodingName = "UTF-8";

            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.GetEncoding(encodingName)))
            {
                T result = JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                return result;
            }
        }

        public static Dictionary<string, object> GetDictionary(this WebResponse response)
        {
            return response.GetObject<Dictionary<string, object>>();
        }
    }
}
