using CommandLine;

namespace Pimix.Apps.FileUtil.Commands {
    [Verb("verify", HelpText = "Verify the file is in compliant with the data stored in server.")]
    class VerifyCommand : PimixCommand {
        [Value(0, Required = true)]
        public string FileUri { get; set; }

        public override int Execute()
            => new InfoCommand {
                FileUri = FileUri,
                VerifyAll = true,
                Update = true
            }.Execute();
    }
}
