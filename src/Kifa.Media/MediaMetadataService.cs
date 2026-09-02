using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using NLog;

namespace Kifa.Media;

public static class MediaMetadataService {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static MediaMetadata Extract(Stream stream, string fileName,
        DateTime? fileModifiedTime = null) {
        var metadata = new MediaMetadata();

        // 1. Check filename patterns (e.g. Android screenshots, macOS screenshots)
        ParseFromFileName(Path.GetFileNameWithoutExtension(fileName), metadata);

        // 2. Read metadata via MetadataExtractor
        try {
            if (stream.CanSeek) {
                stream.Seek(0, SeekOrigin.Begin);
            }

            var directories = ImageMetadataReader.ReadMetadata(stream);

            ReadExifMetadata(directories, metadata);
            ReadVideoMetadata(directories, metadata);
        } catch (Exception ex) {
            Logger.Debug(ex, $"Failed to read EXIF/container metadata from '{fileName}'.");
        }

        // 3. Fallback to file system time if still missing
        if (metadata.CapturedAt == null) {
            metadata.CapturedAt = fileModifiedTime;
        }

        return metadata;
    }

    static readonly Regex AndroidScreenshotRegex = new(
        @"^Screenshot_(\d{4})-?(\d{2})-?(\d{2})-?(\d{2})-?(\d{2})-?(\d{2})(?:-(\d{1,3}))?(?:_([a-zA-Z0-9_\.]+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex MacScreenshotRegex = new(
        @"^(?:Screenshot|截屏)\s+(\d{4})-(\d{2})-(\d{2})\s+(?:at\s+|于\s+)?(\d{2})[.\:](\d{2})[.\:](\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex WindowsScreenshotRegex = new(
        @"^Screenshot\s+(\d{4})-(\d{2})-(\d{2})\s+(\d{2})(\d{2})(\d{2})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex GenericScreenshotRegex = new(
        @"(?:^|[\W_])(?:screenshot|screen_shot|截屏|截图)(?:[\W_]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static readonly Regex StandardCameraFileNameRegex = new(
        @"^(?:IMG_|VID_|PANO_|MMExport_)?(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})(?:[._](\d{1,3}))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static void ParseFromFileName(string baseFileName, MediaMetadata metadata) {
        if (GenericScreenshotRegex.IsMatch(baseFileName)) {
            metadata.IsScreenshot = true;
        }

        var androidMatch = AndroidScreenshotRegex.Match(baseFileName);
        if (androidMatch.Success) {
            metadata.IsScreenshot = true;
            if (int.TryParse(androidMatch.Groups[1].Value, out var year) &&
                int.TryParse(androidMatch.Groups[2].Value, out var month) &&
                int.TryParse(androidMatch.Groups[3].Value, out var day) &&
                int.TryParse(androidMatch.Groups[4].Value, out var hour) &&
                int.TryParse(androidMatch.Groups[5].Value, out var min) &&
                int.TryParse(androidMatch.Groups[6].Value, out var sec)) {
                metadata.CapturedAt = new DateTime(year, month, day, hour, min, sec);
            }

            if (androidMatch.Groups[7].Success && androidMatch.Groups[7].Value.Length > 0) {
                metadata.SubSecond = NormalizeSubSecond(androidMatch.Groups[7].Value);
            }

            if (androidMatch.Groups[8].Success && androidMatch.Groups[8].Value.Length > 0) {
                metadata.AppPackage = androidMatch.Groups[8].Value;
            }

            return;
        }

        var macMatch = MacScreenshotRegex.Match(baseFileName);
        if (macMatch.Success) {
            metadata.IsScreenshot = true;
            if (int.TryParse(macMatch.Groups[1].Value, out var year) &&
                int.TryParse(macMatch.Groups[2].Value, out var month) &&
                int.TryParse(macMatch.Groups[3].Value, out var day) &&
                int.TryParse(macMatch.Groups[4].Value, out var hour) &&
                int.TryParse(macMatch.Groups[5].Value, out var min) &&
                int.TryParse(macMatch.Groups[6].Value, out var sec)) {
                metadata.CapturedAt = new DateTime(year, month, day, hour, min, sec);
            }

            return;
        }

        var winMatch = WindowsScreenshotRegex.Match(baseFileName);
        if (winMatch.Success) {
            metadata.IsScreenshot = true;
            if (int.TryParse(winMatch.Groups[1].Value, out var year) &&
                int.TryParse(winMatch.Groups[2].Value, out var month) &&
                int.TryParse(winMatch.Groups[3].Value, out var day) &&
                int.TryParse(winMatch.Groups[4].Value, out var hour) &&
                int.TryParse(winMatch.Groups[5].Value, out var min) &&
                int.TryParse(winMatch.Groups[6].Value, out var sec)) {
                metadata.CapturedAt = new DateTime(year, month, day, hour, min, sec);
            }

            return;
        }

        var cameraMatch = StandardCameraFileNameRegex.Match(baseFileName);
        if (cameraMatch.Success) {
            if (int.TryParse(cameraMatch.Groups[1].Value, out var year) &&
                int.TryParse(cameraMatch.Groups[2].Value, out var month) &&
                int.TryParse(cameraMatch.Groups[3].Value, out var day) &&
                int.TryParse(cameraMatch.Groups[4].Value, out var hour) &&
                int.TryParse(cameraMatch.Groups[5].Value, out var min) &&
                int.TryParse(cameraMatch.Groups[6].Value, out var sec)) {
                metadata.CapturedAt ??= new DateTime(year, month, day, hour, min, sec);
            }

            if (cameraMatch.Groups[7].Success && cameraMatch.Groups[7].Value.Length > 0) {
                metadata.SubSecond ??= NormalizeSubSecond(cameraMatch.Groups[7].Value);
            }
        }
    }

    static void ReadExifMetadata(System.Collections.Generic.IReadOnlyList<MetadataExtractor.Directory> directories,
        MediaMetadata metadata) {
        var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        if (subIfd != null) {
            if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dto)) {
                metadata.CapturedAt = dto;
            } else if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var dtd)) {
                metadata.CapturedAt ??= dtd;
            }

            var subSec = subIfd.GetString(ExifDirectoryBase.TagSubsecondTimeOriginal) ??
                         subIfd.GetString(ExifDirectoryBase.TagSubsecondTime);
            if (subSec != null) {
                metadata.SubSecond = NormalizeSubSecond(subSec);
            }
        }

        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (ifd0 != null) {
            if (metadata.CapturedAt == null &&
                ifd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dt)) {
                metadata.CapturedAt = dt;
            }

            var make = ifd0.GetString(ExifDirectoryBase.TagMake);
            if (make != null) {
                metadata.Make = CleanString(make);
            }

            var model = ifd0.GetString(ExifDirectoryBase.TagModel);
            if (model != null) {
                metadata.Model = CleanString(model);
            }
        }
    }

    static void ReadVideoMetadata(System.Collections.Generic.IReadOnlyList<MetadataExtractor.Directory> directories,
        MediaMetadata metadata) {
        var qtMovie = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
        if (qtMovie != null) {
            if (qtMovie.TryGetInt64(QuickTimeMovieHeaderDirectory.TagDuration, out var durationUnits) &&
                qtMovie.TryGetInt64(QuickTimeMovieHeaderDirectory.TagTimeScale, out var timeScale) &&
                timeScale > 0) {
                metadata.Duration = TimeSpan.FromSeconds((double) durationUnits / timeScale);
            }

            // Apple QuickTime creation date with timezone often in metadata tags
            var qtMetadata = directories.OfType<QuickTimeMetadataHeaderDirectory>().FirstOrDefault();
            DateTime? appleCreationDate = null;
            if (qtMetadata != null) {
                var rawDate = qtMetadata.Tags.FirstOrDefault(t => t.Name.Contains("Creation Date", StringComparison.OrdinalIgnoreCase))?.Description;
                if (rawDate != null && DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedAppleDate)) {
                    appleCreationDate = parsedAppleDate;
                }
            }

            if (appleCreationDate != null) {
                metadata.CapturedAt = appleCreationDate;
            } else if (qtMovie.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var qtCreated)) {
                // If CreateDate is video finalization time and we have duration, start = end - duration
                metadata.CapturedAt = metadata.Duration != null
                    ? qtCreated - metadata.Duration.Value
                    : qtCreated;
            }
        }
    }

    public static string? NormalizeSubSecond(string? raw) {
        if (raw == null) {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) {
            return null;
        }

        if (digits.Length >= 3) {
            return digits[..3];
        }

        return digits.PadRight(3, '0');
    }

    static string? CleanString(string? val) {
        if (val == null) {
            return null;
        }

        var clean = val.Trim().Trim('\0', ' ', '\t', '\r', '\n');
        return clean.Length > 0 ? clean : null;
    }
}
