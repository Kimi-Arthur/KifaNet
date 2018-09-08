using CommandLine;

namespace Pimix.Apps.SubUtil.Commands {
    [Verb("generate", HelpText = "Generate subtitle.")]
    class GenerateCommand : SubUtilCommand {
        [Option('s', "subtitle", HelpText = "Subtitle input file, can be ASS or SubRip.")]
        public string SubtitleFile { get; set; }

        [Option('c', "comments", HelpText = "Comments input file, in xml format.")]
        public string CommentsFile { get; set; }

        [Option('o', "output", HelpText = "Output file path, should end with .ass.")]
        public string OutputFile { get; set; }

        public override int Execute() {
            return 0;
        }
    }
}
