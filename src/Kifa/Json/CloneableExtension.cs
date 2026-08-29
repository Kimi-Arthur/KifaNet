namespace Kifa;

public static class CloneableExtension {
    public static T Clone<T>(this T data) => data.ToJson().FromJson<T>()!;
}
