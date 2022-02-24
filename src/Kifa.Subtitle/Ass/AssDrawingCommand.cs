using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kifa.Subtitle.Ass; 

public class AssDrawingCommand {
    public string Name { get; set; }
    public List<PointF> Points { get; set; } = new List<PointF>();


    public void Scale(double scaleX, double scaleY) {
        Points = Points.Select(p => new PointF((p.X * scaleX).RoundUp(10),
            (p.Y * scaleY).RoundUp(10))).ToList();
    }

    public override string ToString() => $"{Name} {string.Join(' ', Points.Select(p => $"{p.X} {p.Y}"))}";
}