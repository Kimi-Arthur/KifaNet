using System;
using System.Drawing;
using System.Globalization;
using Pimix.Subtitle.Ass;

namespace Pimix.Bilibili {
    public struct BilibiliComment {
        public static bool UseBannerEffect { get; set; } = false;

        public enum ModeType {
            None,
            Normal,
            Bottom = 4,
            Top = 5,
            Reverse = 6,
            Special = 7,
            Advanced = 9
        }

        public enum PoolType {
            Normal,
            Subtitle,
            Special
        }

        static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(8);

        static readonly int DefaultColor =
            Color.FromArgb(AssStyle.DefaultSemiAlpha, Color.White).ToArgb();

        public string Text { get; set; }

        public TimeSpan VideoTime { get; set; }

        public DateTime PostTime { get; set; }

        public Color? TextColor { get; set; }

        public ModeType Mode { get; set; }

        public PoolType Pool { get; set; }

        public long UserId { get; set; }

        public long CommentId { get; set; }

        public int FontSize { get; set; }

        public BilibiliComment(string property, string text) {
            Text = text;

            var values = property.Split(',');
            VideoTime = TimeSpan.FromSeconds(double.Parse(values[0]));
            Mode = (ModeType) int.Parse(values[1]);
            FontSize = int.Parse(values[2]);
            var alpha = Mode == ModeType.Bottom || Mode == ModeType.Top
                ? 255
                : AssStyle.DefaultSemiAlpha;

            TextColor = Color.FromArgb(alpha, Color.FromArgb(int.Parse(values[3])));
            if (TextColor.Value.ToArgb() == DefaultColor) {
                TextColor = null;
            }

            PostTime = new DateTime(1970, 1, 1).AddSeconds(double.Parse(values[4]));
            Pool = (PoolType) int.Parse(values[5]);
            UserId = long.Parse(values[6], NumberStyles.HexNumber);
            CommentId = long.Parse(values[7]);
        }

        public BilibiliComment WithOffset(TimeSpan offset) {
            var result = this;
            result.VideoTime = result.VideoTime.Add(offset);
            return result;
        }

        public AssDialogue GenerateAssDialogue()
            => UseBannerEffect
                ? new AssDialogue {
                    Start = VideoTime,
                    End = VideoTime + DefaultDuration,
                    Layer = GetLayer(Mode),
                    Text = new AssDialogueText(new AssDialogueTextElement {
                        PrimaryColour = TextColor,
                        Content = Text
                    }),
                    Effect = new AssDialogueBannerEffect {
                        Delay = 1500 / (100 + Text.Length)
                    },
                    Style = GetStyle(Mode)
                }
                : new AssDialogue {
                    Start = VideoTime,
                    End = VideoTime + DefaultDuration,
                    Layer = GetLayer(Mode),
                    Text = new AssDialogueText(new AssDialogueTextElement {
                        PrimaryColour = TextColor,
                        Content = Text
                    }),
                    Style = GetStyle(Mode)
                };

        static int GetLayer(ModeType mode) => mode == ModeType.Normal ? 0 : 1;

        static AssStyle GetStyle(ModeType mode) {
            switch (mode) {
                case ModeType.Normal:
                    return AssStyle.NormalCommentStyle;
                case ModeType.Reverse:
                    return AssStyle.RtlCommentStyle;
                case ModeType.Top:
                    return AssStyle.TopCommentStyle;
                case ModeType.Bottom:
                    return AssStyle.BottomCommentStyle;
                default:
                    return AssStyle.DefaultStyle;
            }
        }
    }
}
