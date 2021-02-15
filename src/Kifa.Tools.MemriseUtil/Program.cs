using System;
using CommandLine;
using Kifa.Tools.MemriseUtil.Commands;

namespace Kifa.Tools.MemriseUtil {
    class Program {
        static int Main(string[] args) =>
            PimixCommand.Run(Parser.Default.ParseArguments<UploadAudioCommand, UploadWordCommand>, args);
    }
}
