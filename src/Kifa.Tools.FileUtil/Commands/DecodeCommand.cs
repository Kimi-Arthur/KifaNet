using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.GameHacking.Files;
using Kifa.Jobs;
using Kifa.Service;
using Newtonsoft.Json;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("decode", HelpText = "Decode file.")]
class DecodeCommand : KifaCommand {
    [Value(0, Required = true, HelpText = "Target file(s) to import.")]
    public IEnumerable<string> FileNames { get; set; }

    public override int Execute(KifaTask? task = null) {
        var files = FileNames.SelectMany(path => new KifaFile(path).List()).ToList();
        var selected = SelectMany(files, file => file.ToString(), "files to decode");
        if (selected.Status != KifaActionStatus.OK) {
            ExecuteItem("files to decode", () => selected);
            return LogSummary();
        }

        foreach (var file in selected.Value) {
            ExecuteItem(file.ToString(), () => DecodeFile(file));
        }

        return LogSummary();
    }

    void DecodeFile(KifaFile file) {
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
}
