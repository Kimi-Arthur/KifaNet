using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Kifa.Service;

public class Link<TDataModel> : JsonSerializable<Link<TDataModel>>, IEquatable<Link<TDataModel>>
    where TDataModel : DataModel, WithModelId<TDataModel> {
    public required string Id { get; init; }

    TDataModel? data;

    // Kept separate from `data != null` so reading/setting Data in C# caches the target
    // without causing it to serialize as an expanded object unless Expand() was explicitly called.
    bool shouldExpand;

    public TDataModel? Data {
        get => data ??= TDataModel.Client.Get(Id);
        set => data = value;
    }

    public TDataModel? Refresh(bool force = false) {
        if (force || data == null || data.NeedRefresh()) {
            data = TDataModel.Client.Get(Id, force
                ? new() {
                    Refresh = true
                }
                : null);
        }

        return data;
    }

    public TDataModel? Expand() {
        shouldExpand = true;
        return Data;
    }

    [return: NotNullIfNotNull(nameof(id))]
    public static implicit operator Link<TDataModel>?(string? id)
        => id == null
            ? null
            : new Link<TDataModel> {
                Id = id
            };

    [return: NotNullIfNotNull(nameof(data))]
    public static implicit operator Link<TDataModel>?(TDataModel? data)
        => data == null
            ? null
            : new Link<TDataModel> {
                Id = data.Id.Checked(),
                Data = data
            };

    [return: NotNullIfNotNull(nameof(link))]
    public static implicit operator string?(Link<TDataModel>? link) => link?.Id;

    public static implicit operator TDataModel?(Link<TDataModel>? link) => link?.Data;

    public string ToJson() => Id;

    // Falls back to Id when Data is null (e.g. target not found or upstream temporarily unreachable)
    // so the known ID is preserved without failing the parent request or emitting null in collections.
    public object ToExpandedJson() => shouldExpand ? (object?) Data ?? Id : Id;

    public static Link<TDataModel>? ReadJson(JsonReader reader, JsonSerializer serializer)
        => reader.TokenType switch {
            JsonToken.Null => null,
            JsonToken.StartObject => serializer.Deserialize<TDataModel>(reader),
            _ => reader.Value?.ToString()
        };

    public bool Equals(Link<TDataModel>? other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }

        if (ReferenceEquals(this, obj)) {
            return true;
        }

        return obj.GetType() == GetType() && Equals((Link<TDataModel>) obj);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
