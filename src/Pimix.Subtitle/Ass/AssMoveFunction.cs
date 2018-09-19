using System.Drawing;

namespace Pimix.Subtitle.Ass {
    public class AssMoveFunction : AssTextFunction {
        public Point Start { get; set; }
        public Point End { get; set; }

        public override string ToString() => $"{{\\move({Start.X}, {Start.Y}, {End.X}, {End.Y})}}";
    }
}
