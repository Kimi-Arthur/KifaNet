using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    [DataModel("bilibili/bangumis")]
    public class BilibiliBangumi {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }
}
