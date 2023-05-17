using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.GameHacking.Files;
using Newtonsoft.Json;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("decode", HelpText = "Decode file.")]
class DecodeCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute() {
        foreach (var file in FileNames.SelectMany(path => new KifaFile(path).List())) {
            using var stream = file.OpenRead();
            var folder = file.Parent.GetFile(file.BaseName);
            if (file.Extension == "lzs") {
                foreach (var (name, data) in LzssFile.GetFiles(stream)) {
                    var target = folder.GetFile(name);
                    target.Delete();
                    target.Write(data);
                }
            } else if (file.Name.StartsWith("msg_") && file.Extension == "bin") {
                var messages = MsgBinFile.GetMessages(stream);
                var target = file.Parent.GetFile(file.BaseName + ".json");
                target.Delete();
                target.Write(JsonConvert.SerializeObject(messages,
                    KifaJsonSerializerSettings.Pretty));
            }
        }

        return 0;
    }
}
