using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    [DataModel("bilibili/tvs")]
    public class BilibiliTv {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }
}
