using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Pimix.IO;

namespace fileutil
{
    [Verb("info", HelpText = "Generate information of the specified file.")]
    class InfoCommand : Command
    {
        [Value(0, Required = true)]
        public string FileUri { get; set; }

        public override int Execute()
        {
            Uri uri = new Uri(FileUri);

            using (var stream = Helpers.GetDataStream(FileUri))
            {
                long len = stream.Length;
                var info = FileInformation.Get(uri.LocalPath).AddProperties(stream, FileProperties.All);
                info.Path = uri.LocalPath;
                if (info.Locations == null)
                    info.Locations = new Dictionary<string, string>();
                info.Locations[$"{uri.Scheme}://{uri.Host}"] = FileUri;
                if (len == info.Size)
                {
                    if (Dryrun)
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
                    }
                    else
                    {
                        FileInformation.Patch(info);
                    }
                }
            }

            return 0;
        }
    }
}
