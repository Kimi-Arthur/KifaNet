using System;
using System.ComponentModel;
using NLog;

namespace Kifa;

public static class Logging {
    public static bool EnableNotice { get; set; } = false;

    public static void Notice(this Logger logger, [Localizable(false)] Func<string> message) {
        if (EnableNotice) {
            logger.Trace(message());
        }
    }

    public static void Notice(this Logger logger, [Localizable(false)] string message) {
        if (EnableNotice) {
            logger.Trace(message);
        }
    }
}
