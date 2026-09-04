using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Api.Files;
using Kifa.IO;
using MetadataExtractor;
using Newtonsoft.Json.Linq;
using NLog;

namespace Kifa.Media;

public static class MediaFileComparator {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static MediaComparisonResult Compare(string file1, string file2, bool deep = false)
        => Compare(new KifaFile(file1, fileInfo: new FileInformation()),
            new KifaFile(file2, fileInfo: new FileInformation()), deep: deep);

    public static MediaComparisonResult Compare(KifaFile file1, KifaFile file2, bool deep = false) {
        if (!file1.Exists()) {
            throw new FileNotFoundException($"File 1 not found: {file1}");
        }

        if (!file2.Exists()) {
            throw new FileNotFoundException($"File 2 not found: {file2}");
        }

        var result = new MediaComparisonResult {
            File1Path = file1.ToString(),
            File2Path = file2.ToString(),
            File1Size = file1.Length,
            File2Size = file2.Length
        };

        // Requirement 0: Bit-by-bit comparison
        var hash1 = (file1.FileInfo?.Sha256 ?? file1.CalculateInfo(FileProperties.Sha256).Sha256)
            ?.ToLowerInvariant();
        var hash2 = (file2.FileInfo?.Sha256 ?? file2.CalculateInfo(FileProperties.Sha256).Sha256)
            ?.ToLowerInvariant();

        result.File1Sha256 = hash1;
        result.File2Sha256 = hash2;

        if (result.File1Size == result.File2Size &&
            result.File1Sha256 != null &&
            result.File1Sha256 == result.File2Sha256) {
            result.IsBitExactMatch = true;
            result.IsContentMatch = true;
            result.MatchLevel = ContentMatchLevel.BitExact;
            return result;
        }

        result.IsBitExactMatch = false;

        using var context1 = new KifaFileLocalContext(file1);
        using var context2 = new KifaFileLocalContext(file2);

        // Requirement 1: Stream / content comparison using ffmpeg / ffprobe
        var probe1 = ProbeFile(context1.LocalPath);
        var probe2 = ProbeFile(context2.LocalPath);

        CompareStreams(context1.LocalPath, context2.LocalPath, probe1, probe2, result, deep);

        // Requirement 2: Metadata fields comparison
        var fields1 = ExtractAllMetadataFields(context1.LocalPath, probe1);
        var fields2 = ExtractAllMetadataFields(context2.LocalPath, probe2);

        var allKeys = fields1.Keys.Union(fields2.Keys).OrderBy(k => k).ToList();
        var differences = new List<MetadataFieldDifference>();

        foreach (var key in allKeys) {
            fields1.TryGetValue(key, out var f1);
            fields2.TryGetValue(key, out var f2);

            var val1 = f1?.Value;
            var val2 = f2?.Value;

            if (val1 != val2) {
                differences.Add(new MetadataFieldDifference {
                    Category = f1?.Category ?? f2?.Category ?? "",
                    Name = f1?.Name ?? f2?.Name ?? "",
                    File1Value = val1,
                    File2Value = val2
                });
            }
        }

        result.AllDifferences = differences;
        if (result.IsContentMatch) {
            result.Differences = differences;
        }

        return result;
    }

    class KifaFileLocalContext : IDisposable {
        public string LocalPath { get; }
        readonly bool isTemporary;

        public KifaFileLocalContext(KifaFile file) {
            if (file.IsLocal) {
                LocalPath = file.GetLocalPath();
                isTemporary = false;
                return;
            }

            var ext = file.Extension != null ? $".{file.Extension}" : ".bin";
            var tempDir = System.IO.Path.GetFullPath(".agent_temp");
            System.IO.Directory.CreateDirectory(tempDir);
            LocalPath = System.IO.Path.Combine(tempDir, $"kifa_media_{Guid.NewGuid():N}{ext}");
            isTemporary = true;

            using var input = file.OpenRead();
            using var output = File.Create(LocalPath);
            input.CopyTo(output);
        }

        public void Dispose() {
            if (isTemporary && File.Exists(LocalPath)) {
                try {
                    File.Delete(LocalPath);
                } catch {
                    // Ignore cleanup error
                }
            }
        }
    }

    static void CompareStreams(string path1, string path2, MediaProbeResult? probe1,
        MediaProbeResult? probe2, MediaComparisonResult result, bool deep) {
        var streams1 = probe1?.Streams.Where(s => !s.IsAttachedPic).ToList() ?? [];
        var streams2 = probe2?.Streams.Where(s => !s.IsAttachedPic).ToList() ?? [];

        if (streams1.Count > 0 && streams1.Count == streams2.Count) {
            var allStreamsMatch = true;
            var allBitstreamMatch = true;

            for (var i = 0; i < streams1.Count; i++) {
                var s1 = streams1[i];
                var s2 = streams2[i];

                var streamResult = new StreamComparisonResult {
                    Index = s1.Index,
                    StreamType = s1.CodecType,
                    Codec = s1.CodecName,
                    Details = s1.GetDetails(),
                    IsAttachedPic = s1.IsAttachedPic
                };

                // 1. Try bitstream comparison with copy
                var bHash1 = GetStreamHash(path1, s1.Index, copy: true);
                var bHash2 = GetStreamHash(path2, s2.Index, copy: true);
                streamResult.File1BitstreamHash = bHash1;
                streamResult.File2BitstreamHash = bHash2;

                if (!deep && bHash1 != null && bHash2 != null && bHash1 == bHash2) {
                    streamResult.IsBitstreamMatch = true;
                    streamResult.IsMatch = true;
                } else if (s1.CodecType != "subtitle") {
                    // 2. Fall back to decoded frames / samples comparison
                    var dHash1 = GetStreamHash(path1, s1.Index, copy: false);
                    var dHash2 = GetStreamHash(path2, s2.Index, copy: false);
                    streamResult.File1DecodedHash = dHash1;
                    streamResult.File2DecodedHash = dHash2;

                    if (dHash1 != null && dHash2 != null && dHash1 == dHash2) {
                        streamResult.IsDecodedMatch = true;
                        streamResult.IsMatch = true;
                        allBitstreamMatch = false;
                    } else {
                        streamResult.IsMatch = false;
                        allStreamsMatch = false;
                        allBitstreamMatch = false;
                    }
                } else {
                    streamResult.IsMatch = false;
                    allStreamsMatch = false;
                    allBitstreamMatch = false;
                }

                result.Streams.Add(streamResult);
            }

            if (allStreamsMatch) {
                result.IsContentMatch = true;
                result.MatchLevel = allBitstreamMatch
                    ? ContentMatchLevel.BitstreamMatch
                    : ContentMatchLevel.DecodedMatch;
                return;
            }
        }

        // Whole file decoded hash fallback
        var fullDecoded1 = GetWholeFileDecodedHash(path1);
        var fullDecoded2 = GetWholeFileDecodedHash(path2);
        if (fullDecoded1 != null && fullDecoded2 != null && fullDecoded1 == fullDecoded2) {
            result.IsContentMatch = true;
            result.MatchLevel = ContentMatchLevel.DecodedMatch;
            result.Streams.Add(new StreamComparisonResult {
                Index = 0,
                StreamType = "media",
                Codec = "decoded",
                IsMatch = true,
                IsDecodedMatch = true,
                File1DecodedHash = fullDecoded1,
                File2DecodedHash = fullDecoded2
            });
            return;
        }

        result.IsContentMatch = false;
        result.MatchLevel = ContentMatchLevel.NoMatch;
    }

    static readonly Regex Sha256Pattern =
        new(@"SHA256=([0-9a-fA-F]{64})", RegexOptions.Compiled);

    static string? GetStreamHash(string path, int streamIndex, bool copy) {
        var copyFlag = copy ? "-c copy " : "";
        var execution = Executor.Run("ffmpeg",
            $"-v error -i \"{path}\" -map 0:{streamIndex} {copyFlag}-f hash -hash sha256 -");
        if (execution.ExitCode != 0) {
            return null;
        }

        var output = execution.StandardOutput != null && execution.StandardOutput.Length > 0
            ? execution.StandardOutput
            : execution.StandardError;
        var match = Sha256Pattern.Match(output);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }

    static string? GetWholeFileDecodedHash(string path) {
        var execution = Executor.Run("ffmpeg", $"-v error -i \"{path}\" -f hash -hash sha256 -");
        if (execution.ExitCode != 0) {
            return null;
        }

        var output = execution.StandardOutput != null && execution.StandardOutput.Length > 0
            ? execution.StandardOutput
            : execution.StandardError;
        var match = Sha256Pattern.Match(output);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }

    static MediaProbeResult? ProbeFile(string path) {
        var execution = Executor.Run("ffprobe",
            $"-v error -show_format -show_streams -of json \"{path}\"");
        if (execution.ExitCode != 0 || execution.StandardOutput == null ||
            execution.StandardOutput.Length == 0) {
            return null;
        }

        try {
            var json = JObject.Parse(execution.StandardOutput);
            var probeResult = new MediaProbeResult();

            if (json["format"] is JObject formatObj) {
                probeResult.FormatName = formatObj["format_name"]?.ToString();
                probeResult.Duration = formatObj["duration"]?.ToString();
                probeResult.BitRate = formatObj["bit_rate"]?.ToString();
                probeResult.Size = formatObj["size"]?.ToString();

                if (formatObj["tags"] is JObject tagsObj) {
                    foreach (var prop in tagsObj.Properties()) {
                        probeResult.FormatTags[prop.Name] = prop.Value.ToString();
                    }
                }
            }

            if (json["streams"] is JArray streamsArr) {
                foreach (var stObj in streamsArr.OfType<JObject>()) {
                    var sInfo = new ProbeStreamInfo {
                        Index = stObj["index"]?.Value<int>() ?? 0,
                        CodecType = stObj["codec_type"]?.ToString() ?? "",
                        CodecName = stObj["codec_name"]?.ToString() ?? "",
                        Profile = stObj["profile"]?.ToString(),
                        PixelFormat = stObj["pix_fmt"]?.ToString(),
                        Width = stObj["width"]?.Value<int>(),
                        Height = stObj["height"]?.Value<int>(),
                        SampleRate = stObj["sample_rate"]?.ToString(),
                        Channels = stObj["channels"]?.Value<int>(),
                        ChannelLayout = stObj["channel_layout"]?.ToString(),
                        FrameRate = stObj["r_frame_rate"]?.ToString(),
                        IsAttachedPic = stObj["disposition"]?["attached_pic"]?.Value<int>() == 1
                    };

                    if (stObj["tags"] is JObject stTags) {
                        foreach (var prop in stTags.Properties()) {
                            sInfo.Tags[prop.Name] = prop.Value.ToString();
                        }
                    }

                    if (stObj["disposition"] is JObject stDisp) {
                        foreach (var prop in stDisp.Properties()) {
                            if (prop.Value.Type == JTokenType.Integer &&
                                prop.Value.Value<int>() != 0) {
                                sInfo.Dispositions[prop.Name] = prop.Value.Value<int>();
                            }
                        }
                    }

                    probeResult.Streams.Add(sInfo);
                }
            }

            return probeResult;
        } catch (Exception ex) {
            Logger.Warn(ex, $"Failed to parse ffprobe JSON output for '{path}'.");
            return null;
        }
    }

    record RawField(string Category, string Name, string Value);

    static Dictionary<string, RawField> ExtractAllMetadataFields(string localPath,
        MediaProbeResult? probe) {
        var map = new Dictionary<string, RawField>();

        void AddField(string category, string name, string? value) {
            if (value == null) {
                return;
            }

            var key = $"[{category}] {name}";
            map[key] = new RawField(category, name, value);
        }

        if (probe != null) {
            if (probe.FormatName != null) {
                AddField("Format", "format_name", probe.FormatName);
            }

            if (probe.Duration != null) {
                AddField("Format", "duration", probe.Duration);
            }

            if (probe.BitRate != null) {
                AddField("Format", "bit_rate", probe.BitRate);
            }

            if (probe.Size != null) {
                AddField("Format", "size", probe.Size);
            }

            foreach (var (k, v) in probe.FormatTags) {
                AddField("Format Tags", k, v);
            }

            foreach (var s in probe.Streams) {
                var streamCat = s.IsAttachedPic
                    ? $"Stream #{s.Index} (Attached Pic)"
                    : $"Stream #{s.Index} ({s.CodecType})";

                AddField(streamCat, "codec_name", s.CodecName);
                if (s.Profile != null) {
                    AddField(streamCat, "profile", s.Profile);
                }

                if (s.PixelFormat != null) {
                    AddField(streamCat, "pix_fmt", s.PixelFormat);
                }

                if (s.Width != null && s.Height != null) {
                    AddField(streamCat, "resolution", $"{s.Width}x{s.Height}");
                }

                if (s.SampleRate != null) {
                    AddField(streamCat, "sample_rate", s.SampleRate);
                }

                if (s.Channels != null) {
                    AddField(streamCat, "channels", s.Channels.ToString());
                }

                if (s.ChannelLayout != null) {
                    AddField(streamCat, "channel_layout", s.ChannelLayout);
                }

                var streamTagsCat = $"Stream #{s.Index} Tags";
                foreach (var (k, v) in s.Tags) {
                    AddField(streamTagsCat, k, v);
                }

                if (s.Dispositions.Count > 0) {
                    var dispCat = $"Stream #{s.Index} Disposition";
                    foreach (var (k, v) in s.Dispositions) {
                        AddField(dispCat, k, v.ToString());
                    }
                }
            }
        }

        try {
            var directories = ImageMetadataReader.ReadMetadata(localPath);
            foreach (var dir in directories) {
                if (dir.Name == "File") {
                    continue;
                }

                foreach (var tag in dir.Tags) {
                    if (tag.Description != null) {
                        AddField(dir.Name, tag.Name, tag.Description);
                    }
                }
            }
        } catch (Exception ex) {
            Logger.Debug(ex, $"Failed to read MetadataExtractor tags from '{localPath}'.");
        }

        return map;
    }

    class MediaProbeResult {
        public string? FormatName { get; set; }
        public string? Duration { get; set; }
        public string? BitRate { get; set; }
        public string? Size { get; set; }
        public Dictionary<string, string> FormatTags { get; set; } = [];
        public List<ProbeStreamInfo> Streams { get; set; } = [];
    }

    class ProbeStreamInfo {
        public int Index { get; set; }
        public string CodecType { get; set; } = "";
        public string CodecName { get; set; } = "";
        public string? Profile { get; set; }
        public string? PixelFormat { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? SampleRate { get; set; }
        public int? Channels { get; set; }
        public string? ChannelLayout { get; set; }
        public string? FrameRate { get; set; }
        public bool IsAttachedPic { get; set; }
        public Dictionary<string, string> Tags { get; set; } = [];
        public Dictionary<string, int> Dispositions { get; set; } = [];

        public string? GetDetails() {
            if (CodecType == "video") {
                var res = Width != null && Height != null ? $"{Width}x{Height}" : null;
                var parts = new List<string?> {
                    CodecName,
                    res,
                    PixelFormat,
                    FrameRate != null ? $"{FrameRate} fps" : null
                };
                return string.Join(", ", parts.Where(p => p != null));
            }

            if (CodecType == "audio") {
                var parts = new List<string?> {
                    CodecName,
                    SampleRate != null ? $"{SampleRate} Hz" : null,
                    Channels != null ? $"{Channels} ch" : null
                };
                return string.Join(", ", parts.Where(p => p != null));
            }

            return CodecName;
        }
    }
}
