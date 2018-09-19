using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public class AssMoveFunction : AssTextFunction {
        public PointF Start { get; set; }
        public PointF End { get; set; }

        public override string ToString() => $"\\move({Start.X},{Start.Y},{End.X},{End.Y})";
    }
}
