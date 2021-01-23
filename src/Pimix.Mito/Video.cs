using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Kifa.Service;

namespace Pimix.Mito {
    public class Video : DataModel {
        public const string ModelId = "mito/videos";

        public string Title { get; set; }
        public List<Actress> Actresses { get; set; } = new List<Actress>();
        public string Description { get; set; }
        public Date Published { get; set; }
        public TimeSpan Length { get; set; }
        public VideoIds VideoIds { get; set; } = new VideoIds();
        public List<string> Categories { get; set; } = new List<string>();
        public VideoType Type { get; set; }

        [JsonIgnore]
        public string TypePrefix =>
            Type switch {
                VideoType.Jav => "/Venus/JAV",
                VideoType.Uncensored => "/Venus/JAV Uncensored",
                VideoType.Vr => "/Venus/JAV VR",
                _ => "/Venus"
            };

        [JsonIgnore] public string Path => $"{TypePrefix}/{Id}";

        [JsonIgnore]
        public List<string> PathsByActress =>
            Actresses.Select(actress => $"{TypePrefix}/#Actress/{actress.Name}/{Id} {Title}").ToList();

        [JsonIgnore]
        public List<string> PathsByCategory => Categories.Select(tag => $"{TypePrefix}/#Category/{tag}/{Id} {Title}").ToList();
    }

    public enum VideoType {
        Jav,
        Uncensored,
        Vr
    }

    public class VideoIds {
        public string DmmId { get; set; }
        public string DmmDvdId { get; set; }
    }
}
