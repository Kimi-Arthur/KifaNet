using NLog;
using Pimix.Service;

namespace Pimix.Languages.German {
    public class Verb : Word {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public new const string ModelId = "languages/german/verbs";

        public override void Fill() {
            var words = GetWords();
            FillWithData(words);
            VerbForms = words.pons.VerbForms;
        }
    }

    public interface VerbServiceClient : PimixServiceClient<Verb> {
    }

    public class VerbRestServiceClient : PimixServiceRestClient<Verb>, VerbServiceClient {
    }
}
