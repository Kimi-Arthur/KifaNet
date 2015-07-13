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

        [Option('v', "verify-all", HelpText = "Verify all verifiable fields of the file along with updating info.")]
        public bool VerifyAll { get; set; }

        [Option('f', "fields-to-verify", HelpText = "Fields to verify. Only 'Size' is verified by default.")]
        public string FieldsToVerify { get; set; } = "Size";

        public FileProperties FilePropertiesToVerify
            => VerifyAll ? FileProperties.AllVerifiable : FileProperties.AllVerifiable & (FileProperties)Enum.Parse(typeof(FileProperties), FieldsToVerify);

        [Option('u', "update", HelpText = "Whether to update result to server.")]
        public bool Update { get; set; } = false;

        public override int Execute()
        {
            Uri uri = new Uri(FileUri);

            using (var stream = Helpers.GetDataStream(FileUri))
            {
                long len = stream.Length;
                var info = FileInformation.Get(uri.LocalPath).RemoveProperties(FilePropertiesToVerify).AddProperties(stream, FileProperties.All);
                info.Path = uri.LocalPath;
                if (info.Locations == null)
                    info.Locations = new Dictionary<string, string>();
                info.Locations[$"{uri.Scheme}://{uri.Host}"] = FileUri;
                var old = FileInformation.Get(uri.LocalPath);
                if (info.CompareProperties(old, FilePropertiesToVerify))
                {
                    if (Update)
                    {
                        FileInformation.Patch(info);
                    }
                    else
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
                    }

                    return 0;
                }
                else
                {
                    Console.WriteLine("Verify failed!");
                    Console.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
                    Console.WriteLine(JsonConvert.SerializeObject(old, Formatting.Indented));
                    return 1;
                }
            }
        }
    }
}
