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

        public string Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string AuthorId { get; set; }
        public string Description { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Tags { get; set; }
        public string Category { get; set; }
        public string Cover { get; set; }
        public DateTime? Uploaded { get; set; }
        public List<BilibiliChat> Pages { get; set; }

        PartModeType partMode;

        [JsonIgnore]
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
