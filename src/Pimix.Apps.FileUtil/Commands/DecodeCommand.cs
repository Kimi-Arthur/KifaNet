﻿using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Newtonsoft.Json;
using Pimix.Api.Files;
using Pimix.Games.Files;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("decode", HelpText = "Decode file.")]
    class DecodeCommand : PimixCommand {
        [Value(0, Required = true, HelpText = "Target file(s) to import.")]
        public IEnumerable<string> FileNames { get; set; }

        public override int Execute() {
            foreach (var file in FileNames.SelectMany(path =>
                new PimixFile(path).List())) {
                var folder = file.Parent.GetFile(file.BaseName);
                if (file.Extension == "lzs") {
                    foreach (var (name, data) in LzssFile.GetFiles(file.OpenRead())) {
                        var target = folder.GetFile(name);
                        target.Delete();
                        target.Write(data);
                    }
                } else if (file.Name.StartsWith("msg_") && file.Extension == "bin") {
                    var messages = MsgBinFile.GetMessages(file.OpenRead());
                    var target = file.Parent.GetFile(file.BaseName + ".json");
                    target.Delete();
                    target.Write(JsonConvert.SerializeObject(messages, Defaults.PrettyJsonSerializerSettings));
                }
            }

            return 0;
        }
    }
}