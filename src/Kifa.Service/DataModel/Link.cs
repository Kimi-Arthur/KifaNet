using System;

namespace Kifa.Service;

// Unlimited linking not supported now.
public class Link<TDataModel> : JsonSerializable, IEquatable<Link<TDataModel>>
    where TDataModel : DataModel, new() {
    public string Id { get; set; }

    public Link(TDataModel data) {
        Id = data.Id;
    }

    public Link(string id) {
        Id = id;
    }

    TDataModel Get()
        => new() {
            Id = Id
        };

    public static implicit operator Link<TDataModel>(string id) => new(id);

    public static implicit operator string(Link<TDataModel> data) => data.ToJson();

    public string ToJson() => Id;

    public void FromJson(string data) => Id = data;

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
