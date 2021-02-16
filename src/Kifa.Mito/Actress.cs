using Kifa.Service;

namespace Kifa.Mito {
    public class Actress : DataModel {
        public const string ModelId = "mito/actresses";

        public string Name { get; set; }
        public PersonalData PersonalData { get; set; } = new PersonalData();
        public JavIds Ids { get; set; } = new JavIds();
        public SocialMediaIds SocialMedia { get; set; } = new SocialMediaIds();
    }

    public class SocialMediaIds {
        public string Instagram { get; set; }
        public string Twitter { get; set; }
    }

    public class JavIds {
        public string PrimaryId { get; set; }
        public string DmmId { get; set; }
        public string FalenoId { get; set; }
        public string S1Id { get; set; }
    }

    public class PersonalData {
        public Date Birthday { get; set; }
        public int? Height { get; set; }
        public int? Bust { get; set; }
        public int? Waist { get; set; }
        public int? Hips { get; set; }
        public string Cup { get; set; }
    }
}
