using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    [DataModel("bilibili/uploaders")]
    public class BilibiliUploader {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }
}
