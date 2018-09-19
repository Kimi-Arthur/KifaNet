using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public class AssDialogueTextElement {
        public AssTextFunction Function { get; set; } = new AssTextFunction();

        public string Content { get; set; }

        public bool? Bold { get; set; }

        public bool? Italic { get; set; }

        public bool? Underline { get; set; }

        public bool? StrikeOut { get; set; }

        public int? Border { get; set; }

        public int? Shadow { get; set; }

        public bool? BlurEdges { get; set; }

        public string FontName { get; set; }

        public int? FontSize { get; set; }

        public int? FontSizePercentX { get; set; }

        public int? FontSizePercentY { get; set; }

        public int? FontSpace { get; set; }

        public int? FontRotationX { get; set; }

        public int? FontRotationY { get; set; }

        public int? FontRotationZ { get; set; }

        public Color? PrimaryColour { get; set; } = null;

        public Color? SecondaryColour { get; set; }

        public Color? OutlineColour { get; set; }

        public Color? BackColour { get; set; }

        public static implicit operator AssDialogueTextElement(string s)
            => new AssDialogueTextElement {Content = s};

        public override string ToString() {
            var styleText = Function.ToString();
            styleText += GenerateAssTextForAttribute("b", Bold);
            styleText += GenerateAssTextForAttribute("i", Italic);
            styleText += GenerateAssTextForAttribute("u", Underline);
            styleText += GenerateAssTextForAttribute("s", StrikeOut);
            styleText += GenerateAssTextForAttribute("bord", Border);
            styleText += GenerateAssTextForAttribute("shad", Shadow);
            styleText += GenerateAssTextForAttribute("be", BlurEdges);
            styleText += GenerateAssTextForAttribute("fn", FontName);
            styleText += GenerateAssTextForAttribute("fs", FontSize);
            styleText += GenerateAssTextForAttribute("fscx", FontSizePercentX);
            styleText += GenerateAssTextForAttribute("fscy", FontSizePercentY);
            styleText += GenerateAssTextForAttribute("fsp", FontSpace);
            styleText += GenerateAssTextForAttribute("frx", FontRotationX);
            styleText += GenerateAssTextForAttribute("fry", FontRotationY);
            styleText += GenerateAssTextForAttribute("frz", FontRotationZ);
            styleText += GenerateAssTextForAttribute("1", PrimaryColour);
            styleText += GenerateAssTextForAttribute("2", SecondaryColour);
            styleText += GenerateAssTextForAttribute("3", OutlineColour);
            styleText += GenerateAssTextForAttribute("4", BackColour);
            return (!string.IsNullOrEmpty(styleText) ? $"{{{styleText}}}" : "") + Content;
        }

        static string GenerateAssTextForAttribute(string name, bool? value)
            => value.HasValue ? $"\\{name}{(value.Value ? "1" : "0")}" : "";

        static string GenerateAssTextForAttribute(string name, int? value)
            => value.HasValue ? $"\\{name}{value.Value}" : "";

        static string GenerateAssTextForAttribute(string name, string value)
            => !string.IsNullOrEmpty(value) ? $"\\{name}{value}" : "";

        static string GenerateAssTextForAttribute(string name, Color? value)
            => value.HasValue
                ? $"\\{name}a&H{255 - value.Value.A:X2}&"
                  + $"\\{name}c&H{value.Value.B:X2}{value.Value.G:X2}{value.Value.R:X2}&"
                : "";
    }
}
