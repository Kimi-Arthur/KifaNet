using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Kifa.IO;

public class FileLocation : JsonSerializable {
    #region public late string ServerType { get; set; }

    string? serverType;

    public string ServerType {
        get => Late.Get(serverType);
        set => Late.Set(ref serverType, value);
    }

    #endregion

    #region public late string ServerId { get; set; }

    string? serverId;

    public string ServerId {
        get => Late.Get(serverId);
        set => Late.Set(ref serverId, value);
    }

    #endregion

    #region public late string Path { get; set; }

    string? path;

    public string Path {
        get => Late.Get(path);
        set => Late.Set(ref path, value);
    }

    #endregion

    static readonly Regex ServerPattern = new(@"^([^:]+):([^/]*)$");

    [JsonIgnore]
    [YamlIgnore]
    public string Server {
        get => $"{ServerType}:{ServerId}";
        set {
            var match = ServerPattern.Match(value);
            if (!match.Success) {
                throw new ArgumentException($"Location id '{value}' not conforming server pattern.",
                    nameof(value));
            }

            ServerType = match.Groups[1].Value;
            ServerId = match.Groups[2].Value;
        }
    }

    static readonly Regex LocationPattern = new(@"^([^:]+):([^/]*)(/.*)$");

    public FileLocation(string id) {
        var match = LocationPattern.Match(id);
        if (!match.Success) {
            throw new ArgumentException($"Location id '{id}' not conforming location pattern.",
                nameof(id));
        }

        ServerType = match.Groups[1].Value;
        ServerId = match.Groups[2].Value;
        Path = match.Groups[3].Value;
    }

    public static implicit operator FileLocation(string data) => new(data);

    public override string ToString() => ToJson();

    public string ToJson() => $"{Server}{Path}";

    public override int GetHashCode() => $"{ServerType}:{ServerId}{Path}".GetHashCode();
}
