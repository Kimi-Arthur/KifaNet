using CommandLine;

namespace Kifa.Tools.MediaUtil.Commands; 


[Verb("cover", HelpText = "[Not implemented] Add cover to media file.")]
public class AddCoverCommand : KifaCommand {
    public override int Execute() => throw new NotImplementedException();
}
