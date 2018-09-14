using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pimix.Service;
using Pimix.Subtitle.Ass;

namespace Pimix.Bilibili {
    [DataModel("bilibili/videos")]
    public class BilibiliVideo {
        public enum PartModeType {
            SinglePartMode,
            ContinuousPartMode,
            ParallelPartMode
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("uploaded")]
        public DateTime Uploaded { get; set; }

        [JsonProperty("pages")]
        public List<BilibiliChat> Pages { get; set; }

        PartModeType partMode;

        public PartModeType PartMode {
            get => partMode;
            set {
                partMode = value;
                if (partMode == PartModeType.ContinuousPartMode) {
                    var offset = TimeSpan.Zero;
                    foreach (var part in Pages) {
                        part.ChatOffset = offset;
                        offset += part.Duration;
                    }
                } else {
                    foreach (var part in Pages) {
                        part.ChatOffset = TimeSpan.Zero;
                    }
                }
            }
        }

        public static BilibiliVideo Get(string id) => PimixService.Get<BilibiliVideo>(id);

        public AssDocument GenerateAssDocument() {
            var result = new AssDocument();
            result.Sections.Add(new AssScriptInfoSection
                {Title = Title, OriginalScript = "Bilibili"});
            result.Sections.Add(new AssStylesSection
                {Styles = new List<AssStyle> {AssStyle.DefaultStyle}});
            var events = new AssEventsSection();
            result.Sections.Add(events);

            foreach (var part in Pages)
            foreach (var comment in part.Comments) {
                events.Events.Add(comment.GenerateAssDialogue());
            }

            return result;
        }
    }
}
