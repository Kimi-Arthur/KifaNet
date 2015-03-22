using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
