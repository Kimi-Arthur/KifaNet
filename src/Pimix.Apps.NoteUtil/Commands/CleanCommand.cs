using CommandLine;
using VerbForms = System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType, System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

namespace Pimix.Apps.NoteUtil.Commands {
    [Verb("clean", HelpText = "Clean up note files.")]
    public class CleanCommand : PimixCommand {
        public override int Execute() {
            return 0;
        }
    }
}
