using System;

namespace Kifa;

public static class ObjectExtensions {
    public static T With<T>(this T data, Action<T> update) where T : class {
        update(data);
        return data;
    }
}
