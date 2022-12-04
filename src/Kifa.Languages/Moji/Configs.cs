namespace Kifa.Languages.Moji;

class Configs {
    #region public late static string SessionToken { get; set; }

    static string? sessionToken;

    public static string SessionToken {
        get => Late.Get(sessionToken);
        set => Late.Set(ref sessionToken, value);
    }

    #endregion

    #region public late static string ApplicationId { get; set; }

    static string? applicationId;

    public static string ApplicationId {
        get => Late.Get(applicationId);
        set => Late.Set(ref applicationId, value);
    }

    #endregion
}
