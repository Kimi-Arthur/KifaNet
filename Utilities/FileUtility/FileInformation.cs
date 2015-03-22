using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pimix.Utilities
{
    public class FileInformation
    {
        [JsonProperty("size")]
        public long? Size { get; set; }

        [JsonProperty("file_path")]
        public string Path { get; set; }
    }
}
