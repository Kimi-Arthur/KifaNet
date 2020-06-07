using System;
using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Mito {
    public class Video : DataModel {
        public const string ModelId = "mito/videos";

        public string Title { get; set; }
        public List<Actress> Actresses { get; set; }
        public string Description { get; set; }
        public Date Published { get; set; }
        public TimeSpan Length { get; set; }
    }
}
