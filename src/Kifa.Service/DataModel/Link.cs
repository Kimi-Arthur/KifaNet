using System;
using System.Diagnostics.CodeAnalysis;

namespace Kifa.Service;

// Unlimited linking not supported now.
public class Link<TDataModel> : JsonSerializable, IEquatable<Link<TDataModel>>
    where TDataModel : DataModel, WithModelId<TDataModel> {
    public required string Id { get; set; }

    TDataModel? data;

    public TDataModel? Data {
        get {
            if (data == null || data.NeedRefresh()) {
                data = TDataModel.Client.Get(Id);
            }

            return data;
        }
        set => data = value;
    }

    [return: NotNullIfNotNull(nameof(id))]
    public static implicit operator Link<TDataModel>?(string? id)
        => id == null
            ? null
            : new Link<TDataModel> {
                Id = id
            };

    public static implicit operator Link<TDataModel>(TDataModel data)
        => new() {
            Id = data.Id,
            Data = data
        };

    public static implicit operator string(Link<TDataModel> link) => link.Id;

    public static implicit operator TDataModel?(Link<TDataModel> link) => link.Data;

    public string ToJson() => Id;

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
