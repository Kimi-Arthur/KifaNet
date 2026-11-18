using System;

namespace Kifa.Service;

public static class RefreshIntervals {
    // Model does not support upstream Fill(); stored as-is.
    public static readonly TimeSpan? None = null;

    // Re-check upstream on every Get.
    public static readonly TimeSpan Zero = TimeSpan.Zero;

    // Fill once on initial creation; never auto-refresh unless Refresh = true or ForceRefreshBefore.
    public static readonly TimeSpan Infinity = TimeSpan.MaxValue;

    // 10-minute grace window to avoid repeated scrapes in the same session (playlists, TvShow, Anime).
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    public static readonly TimeSpan Day = TimeSpan.FromDays(1);
    public static readonly TimeSpan Week = TimeSpan.FromDays(7);
    public static readonly TimeSpan Month = TimeSpan.FromDays(30);
    public static readonly TimeSpan Year = TimeSpan.FromDays(365);
}
