using System;
using System.Collections.Generic;
using System.Linq;
using Kifa.Subtitle.Ass;
using Svg;

namespace Kifa.Bilibili; 

public class BilibiliMask {
    readonly List<(TimeSpan start, SvgImage mask)> Masks = new List<(TimeSpan start, SvgImage mask)>();

    public void ProcessDialogues(List<AssDialogue> dialogs) {
        var sortedDialogs = dialogs.OrderBy(d => d.Start).ToList();
        var currentMask = 0;
        foreach (var dialog in sortedDialogs) {
            currentMask = AddMask(dialog, currentMask);
        }
    }

    int AddMask(AssDialogue dialog, int currentMask) {
        while (currentMask + 1 < Masks.Count && Masks[currentMask + 1].start < dialog.Start) {
            currentMask++;
        }

        var clipElements = new AssDialogueControlTextElement();

        if (Masks[currentMask].start < dialog.Start) {
            clipElements.Elements.Add(GetDrawing(Masks[currentMask].mask));
        }

        foreach (var mask in Masks.Skip(currentMask)) {
        }

        dialog.Text.TextElements.Insert(0, clipElements);

        return currentMask;
    }

    DrawingClipFunction GetDrawing(SvgImage image) => new DrawingClipFunction();
}