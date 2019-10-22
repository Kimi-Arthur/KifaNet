using System;
using System.Collections.Generic;
using System.Drawing;
using Pimix.Subtitle.Ass;

namespace Pimix.Subtitle.Srt {
    public class SrtTextElement {
        public string Content { get; set; }

        public bool Bold { get; set; }

        public bool Italic { get; set; }

        public bool Underline { get; set; }

        public Color? FontColor { get; set; }

        public AssDialogueText ToAss() {
            var controlElement = new AssDialogueControlTextElement();
            if (Bold) {
                controlElement.Elements.Add(new BoldStyle());
            }

            if (Italic) {
                controlElement.Elements.Add(new ItalicStyle());
            }

            if (Underline) {
                controlElement.Elements.Add(new UnderlineStyle());
            }

            if (FontColor.HasValue) {
                var c = new PrimaryColourStyle {
                    Value = FontColor.Value
                };
                controlElement.Elements.Add(c);
            }

            return new AssDialogueText {
                TextElements = new List<AssDialogueTextElement> {
                    controlElement,
                    new AssDialogueRawTextElement {
                        Content = string.Join("\\N",
                            Content.Split(new[] {
                                    "\n", "\n"
                                },
                                StringSplitOptions.RemoveEmptyEntries))
                    }
                }
            };
        }

        public override string ToString() {
            var s = Content;
            if (Bold) {
                s = $"<b>{s}</b>";
            }

            if (Italic) {
                s = $"<i>{s}</i>";
            }

            if (Underline) {
                s = $"<u>{s}</u>";
            }

            if (FontColor.HasValue) {
                s = $"<font color=\"{FontColor.Value.R:X2}" +
                    $"{FontColor.Value.G:X2}" +
                    $"{FontColor.Value.B:X2}\">{s}</font>";
            }

            return s;
        }
    }
}
