using System;
using NLog;
using Pimix.Service;
using VerbForms = System.Collections.Generic.Dictionary<Pimix.Languages.German.VerbFormType, System.Collections.Generic.Dictionary<Pimix.Languages.German.Person, string>>;

namespace Pimix.Languages.German {
    public class Verb : Word {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public new const string ModelId = "languages/german/verbs";

        public override WordType Type => WordType.Verb;

        public VerbForms VerbForms { get; set; } = new VerbForms();

        public override void Fill() {
            var wiki = new Word();
            try {
                wiki = new DeWiktionaryClient().GetWord(Id);
            } catch (Exception ex) {
                logger.Warn($"Failed to get wiki word for {Id}");
            }

            var pons = new Verb();
            try {
                pons = new PonsClient().GetWord(Id) as Verb;
            } catch (Exception ex) {
                logger.Warn($"Failed to get pons word for {Id}");
            }

            var duden = new DudenClient().GetWord(Id);

            FillWithData(wiki, pons, duden);

            VerbForms = pons.VerbForms;
        }
    }

    public enum VerbFormType {
        IndicativePresent
    }

    public enum Person {
        Ich,
        Du,
        Er,
        Wir,
        Ihr,
        Sie
    }

    public interface VerbServiceClient : PimixServiceClient<Verb> {
    }

    public class VerbRestServiceClient : PimixServiceRestClient<Verb>, VerbServiceClient {
    }
}
