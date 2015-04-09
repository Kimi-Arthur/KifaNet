using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pimix.Service
{
    public abstract class DataModel
    {
        public virtual string Id { get; set; }

        public static void POST(DataModel data, string id = null)
        {
            id = id ?? data.Id;
            string content = JsonConvert.SerializeObject(data, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
