using System;
using System.Linq;
using CommandLine;
using Kifa.Api.Files;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.FileUtil.Commands;

[Verb("touch", HelpText = "Touch file.")]
class TouchCommand : KifaCommand {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Value(0, Required = true, MetaName = "File URL")]
    public string FileUri { get; set; }

    public override int Execute() {
        var target = new KifaFile(FileUri);

        var files = FileInformation.Client.ListFolder(target.Path, true);
        if (files.Count > 0) {
            foreach (var file in files) {
                Console.WriteLine(file);
            }

            Console.Write($"Confirm touching the {files.Count} files above?");
            Console.ReadLine();

            return files.Select(f => TouchFile(new KifaFile(target.Host + f))).Max();
        }

        return TouchFile(target);
    }

    int TouchFile(KifaFile target) {
        if (target.Exists()) {
            Logger.Info($"{target} already exists!");
            return 0;
        }

        target.Touch();

        if (target.Exists()) {
            Logger.Info($"{target} is successfully touched!");
            return 0;
        }

        Logger.Fatal($"{target} doesn't exist unexpectedly!");
        return 2;
    }
}
