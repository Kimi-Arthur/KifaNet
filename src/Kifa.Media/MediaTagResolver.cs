using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;

namespace Kifa.Media;

public static class MediaTagResolver {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static string? ResolveSourceTag(MediaMetadata metadata,
        Func<string, string, string?>? confirmPrompt = null) {
        if (metadata.IsScreenshot) {
            if (metadata.AppPackage != null) {
                return ResolveAppTag(metadata.AppPackage, confirmPrompt);
            }

            return "shot";
        }

        if (metadata.Make != null || metadata.Model != null) {
            return ResolveDeviceTag(metadata.Make, metadata.Model, confirmPrompt);
        }

        return null;
    }

    public static string FormatDeviceId(string? make, string? model) {
        var cleanMake = (make ?? "").Trim().ToLowerInvariant();
        var cleanModel = (model ?? "").Trim().ToLowerInvariant();

        if (cleanMake.Length == 0) {
            return cleanModel;
        }

        if (cleanModel.StartsWith(cleanMake)) {
            return cleanModel;
        }

        return $"{cleanMake}/{cleanModel}";
    }

    public static string? ResolveDeviceTag(string? make, string? model,
        Func<string, string, string?>? confirmPrompt = null) {
        var id = FormatDeviceId(make, model);
        if (id.Length == 0) {
            return null;
        }

        try {
            var existing = DeviceTag.Client.Get(id);
            if (existing != null && existing.Tag != null) {
                return existing.Tag;
            }
        } catch (Exception ex) {
            Logger.Debug(ex, $"Failed to fetch DeviceTag for {id} from RPC client.");
        }

        var proposed = ProposeDeviceTag(make, model);
        var finalTag = proposed;

        if (confirmPrompt != null) {
            var deviceDisplay = $"{make} {model}".Trim();
            var userTag = confirmPrompt(
                $"New camera device detected: '{deviceDisplay}' (id: '{id}'). Accept or specify tag:",
                proposed);
            if (userTag != null && userTag.Trim().Length > 0) {
                finalTag = userTag.Trim();
            }
        }

        try {
            DeviceTag.Client.Set(new DeviceTag {
                Id = id,
                Tag = finalTag,
                Manufacturer = make,
                ModelName = model
            });
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to persist DeviceTag '{id}' -> '{finalTag}' to RPC service.");
        }

        return finalTag;
    }

    static readonly Regex IPhoneRegex = new(@"iphone\s*(\d+)?\s*(pro\s*max|pro|plus|mini|se)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex SonyModelRegex = new(@"ilce-([a-z0-9]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex CanonEosRegex = new(@"eos\s*([a-z0-9]+)(?:\s*mark\s*([ivx0-9]+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex PixelRegex = new(@"pixel\s*(\d+)(?:\s*(pro\s*xl|pro|a|xl))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Dictionary<string, string> KnownDeviceMap =
        new(StringComparer.OrdinalIgnoreCase) {
            { "fc3582", "mini4p" }, // DJI Mini 4 Pro
            { "fc3411", "air3" },   // DJI Air 3
            { "fc7303", "mavic3" }  // DJI Mavic 3
        };

    public static string ProposeDeviceTag(string? make, string? model) {
        var m = (model ?? "").Trim();
        var mk = (make ?? "").Trim();

        if (KnownDeviceMap.TryGetValue(m, out var directTag)) {
            return directTag;
        }

        var iphoneMatch = IPhoneRegex.Match(m);
        if (iphoneMatch.Success && (mk.Contains("apple", StringComparison.OrdinalIgnoreCase) ||
                                    m.Contains("iphone", StringComparison.OrdinalIgnoreCase))) {
            var num = iphoneMatch.Groups[1].Value;
            var variant = iphoneMatch.Groups[2].Value.Replace(" ", "").ToLowerInvariant();
            var shortVariant = variant switch {
                "promax" => "pm",
                "pro" => "p",
                "plus" => "plus",
                "mini" => "mini",
                "se" => "se",
                _ => ""
            };
            return $"ip{num}{shortVariant}";
        }

        var sonyMatch = SonyModelRegex.Match(m);
        if (sonyMatch.Success) {
            return $"a{sonyMatch.Groups[1].Value.ToLowerInvariant()}";
        }

        var canonMatch = CanonEosRegex.Match(m);
        if (canonMatch.Success) {
            var modelNum = canonMatch.Groups[1].Value.ToLowerInvariant();
            var mark = canonMatch.Groups[2].Value.ToLowerInvariant();
            var markSlug = mark switch {
                "ii" => "m2",
                "iii" => "m3",
                "iv" => "m4",
                "v" => "m5",
                _ => mark.Length > 0 ? $"m{mark}" : ""
            };
            return $"{modelNum}{markSlug}";
        }

        var pixelMatch = PixelRegex.Match(m);
        if (pixelMatch.Success) {
            var gen = pixelMatch.Groups[1].Value;
            var variant = pixelMatch.Groups[2].Value.Replace(" ", "").ToLowerInvariant();
            var shortVariant = variant switch {
                "proxl" => "pxl",
                "pro" => "p",
                "xl" => "xl",
                "a" => "a",
                _ => ""
            };
            return $"p{gen}{shortVariant}";
        }

        if (m.StartsWith("X-", StringComparison.OrdinalIgnoreCase) ||
            m.StartsWith("X100", StringComparison.OrdinalIgnoreCase)) {
            return m.Replace("-", "").Replace(" ", "").ToLowerInvariant();
        }

        var candidate = m.Length > 0 ? m : mk;
        if (candidate.StartsWith(mk, StringComparison.OrdinalIgnoreCase) && candidate.Length > mk.Length) {
            candidate = candidate[mk.Length..].Trim();
        }

        var clean = new string(candidate.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return clean.Length > 8 ? clean[..8] : (clean.Length > 0 ? clean : "cam");
    }

    public static string ResolveAppTag(string package,
        Func<string, string, string?>? confirmPrompt = null) {
        var cleanPackage = package.Trim().ToLowerInvariant();

        try {
            var existing = AppTag.Client.Get(cleanPackage);
            if (existing != null && existing.Tag != null) {
                return existing.Tag;
            }
        } catch (Exception ex) {
            Logger.Debug(ex, $"Failed to fetch AppTag for {cleanPackage} from RPC client.");
        }

        var proposed = ProposeAppTag(cleanPackage);
        var finalTag = proposed;

        if (confirmPrompt != null) {
            var userTag = confirmPrompt(
                $"New screenshot app detected: '{cleanPackage}'. Accept or specify tag:",
                proposed);
            if (userTag != null && userTag.Trim().Length > 0) {
                finalTag = userTag.Trim();
            }
        }

        try {
            AppTag.Client.Set(new AppTag {
                Id = cleanPackage,
                Tag = finalTag,
                DisplayName = proposed
            });
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to persist AppTag '{cleanPackage}' -> '{finalTag}' to RPC service.");
        }

        return finalTag;
    }

    static readonly Dictionary<string, string> KnownAppMap =
        new(StringComparer.OrdinalIgnoreCase) {
            { "com.tencent.mm", "wechat" },
            { "com.sina.weibo", "weibo" },
            { "tv.danmaku.bili", "bilibili" },
            { "com.bilibili.app.in", "bilibili" },
            { "com.ss.android.ugc.aweme", "douyin" },
            { "com.zhiliaoapp.musically", "tiktok" },
            { "com.android.chrome", "chrome" },
            { "org.chromium.chrome", "chrome" },
            { "com.twitter.android", "twitter" },
            { "com.zhihu.android", "zhihu" },
            { "com.eg.android.AlipayGphone", "alipay" },
            { "com.taobao.taobao", "taobao" },
            { "com.jingdong.app.mall", "jd" },
            { "com.coolapk.market", "coolapk" },
            { "com.netease.cloudmusic", "neteasemusic" }
        };

    public static string ProposeAppTag(string package) {
        if (KnownAppMap.TryGetValue(package, out var tag)) {
            return tag;
        }

        var lastSegment = package.Split('.').LastOrDefault() ?? package;
        var clean = new string(lastSegment.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return clean.Length > 0 ? clean : "app";
    }
}
