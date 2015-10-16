using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pimix
{
    public static class WebResponseExtensions
    {
        public static Dictionary<string, string> EncodingNameFixes { get; set; } =
            new Dictionary<string, string> {["utf8"] = "UTF-8" };

        static string GetString(WebResponse response)
        {
            var resp = response as HttpWebResponse;
            string encodingName = resp.ContentEncoding;
            if (string.IsNullOrEmpty(encodingName))
                encodingName = resp.CharacterSet;
            if (string.IsNullOrEmpty(encodingName))
                encodingName = "UTF-8";

            // Wrongly encoding name handling.
            encodingName = EncodingNameFixes.GetValueOrDefault(encodingName, encodingName);

            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.GetEncoding(encodingName)))
            {
                return sr.ReadToEnd();
            }
        }

        public static JToken GetJToken(this WebResponse response)
            => JToken.Parse(GetString(response));

        public static T GetObject<T>(this WebResponse response)
            => JsonConvert.DeserializeObject<T>(GetString(response));

        public static Dictionary<string, object> GetDictionary(this WebResponse response)
            => response.GetObject<Dictionary<string, object>>();
    }
}
