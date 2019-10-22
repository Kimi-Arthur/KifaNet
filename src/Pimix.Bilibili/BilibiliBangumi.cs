using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliBangumi : DataModel {
        public const string ModelId = "bilibili/bangumis";

        static PimixServiceClient<BilibiliBangumi> client;

        public static PimixServiceClient<BilibiliBangumi> Client => client ??= new PimixServiceRestClient<BilibiliBangumi>();

        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }
}
