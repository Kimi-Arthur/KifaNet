namespace Kifa.Languages.Moji;

class Configs {
    public static string SessionToken {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }

    public static string ApplicationId {
        get => Late.Get(field);
        set => Late.Set(ref field, value);
    }
}
