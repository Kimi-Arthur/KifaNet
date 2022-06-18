using System.Collections.Generic;
using NLog;

namespace Kifa.Subtitle.Ass;

public class AssDialogueEffect {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public virtual string EffectType => null;
    public virtual IEnumerable<string> EffectParameters => null;

    public static AssDialogueEffect Parse(string content) {
        if (content.Length == 0) {
            return new AssDialogueEffect();
        }

        var segments = content.Split(';');
        switch (segments[0]) {
            case AssDialogueBannerEffect.EffectTypeName:
                return new AssDialogueBannerEffect();
            default:
                Logger.Warn("Unexpected dialogue effect type: {0}", content);
                return null;
        }
    }

    public override string ToString()
        => EffectType == null ? "" : $"{EffectType};{string.Join(";", EffectParameters)}";
}
