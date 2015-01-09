using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliBiliAssGenerator.Bilibili
{
    public struct BilibiliComment
    {
        public enum ModeType
        {
            None,
            Normal,
            Bottom = 4,
            Top = 5,
            Reverse = 6,
            Special = 7,
            Advanced = 9
        }

        public enum PoolType
        {
            Normal,
            Subtitle,
            Special
        }

        public string Text { get; set; }
        public TimeSpan VideoTime { get; set; }
        public DateTime PostTime { get; set; }
        public Color TextColor { get; set; }
        public ModeType Mode { get; set; }
        public PoolType Pool { get; set; }
        public long UserId { get; set; }
        public long CommentId { get; set; }
        public int FontSize { get; set; }
        public BilibiliComment(string property, string text)
        {
            Text = text;

            var values = property.Split(',');
            VideoTime = TimeSpan.FromSeconds(double.Parse(values[0]));
            Mode = (ModeType)int.Parse(values[1]);
            FontSize = int.Parse(values[2]);
            TextColor = Color.FromArgb(int.Parse(values[3]));
            PostTime = new DateTime(1970, 1, 1).AddSeconds(double.Parse(values[4]));
            Pool = (PoolType)int.Parse(values[5]);
            UserId = long.Parse(values[6], NumberStyles.HexNumber);
            CommentId = long.Parse(values[7]);
        }

        public BilibiliComment WithOffset(TimeSpan offset)
        {
            var result = this;
            result.VideoTime.Add(offset);
            return result;
        }
    }
}
