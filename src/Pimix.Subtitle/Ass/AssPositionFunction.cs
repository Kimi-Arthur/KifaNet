using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public class AssPositionFunction : AssTextFunction {
        public PointF Position { get; set; }

        public override string ToString() => $"\\pos({Position.X},{Position.Y})";
    }
}
