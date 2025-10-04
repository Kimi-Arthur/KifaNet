using System;
using System.Collections.Generic;

namespace Kifa;

public static class LineDiffer {
    public static List<string> DiffLines(List<string> oldLines, List<string> newLines) {
        var f = new int[oldLines.Count + 1, newLines.Count + 1];
        for (var i = 0; i < oldLines.Count; i++) {
            for (var j = 0; j < newLines.Count; j++) {
                if (oldLines[i] == newLines[j]) {
                    f[i + 1, j + 1] = f[i, j] + 1;
                } else {
                    f[i + 1, j + 1] = Math.Max(f[i, j + 1], f[i + 1, j]);
                }
            }
        }

        var finalLines = new List<string>();
        var oldIndex = oldLines.Count;
        var newIndex = newLines.Count;
        while (oldIndex > 0 || newIndex > 0) {
            if (oldIndex > 0 && f[oldIndex, newIndex] == f[oldIndex - 1, newIndex]) {
                finalLines.Add("- " + oldLines[oldIndex - 1]);
                oldIndex--;
                continue;
            }

            if (newIndex > 0 && f[oldIndex, newIndex] == f[oldIndex, newIndex - 1]) {
                finalLines.Add("+ " + newLines[newIndex - 1]);
                newIndex--;
                continue;
            }

            finalLines.Add("  " + oldLines[oldIndex - 1]);
            newIndex--;
            oldIndex--;
        }

        finalLines.Reverse();
        return finalLines;
    }
}
