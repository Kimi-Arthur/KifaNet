using System;
using System.IO;
using Kifa.IO;

namespace Kifa.Cloud.Telegram;

public class TelegramStorageClient : StorageClient, CanCreateStorageClient {
    #region public late static long ApiId { get; set; }

    static int? apiId;

    public static int ApiId {
        get => Late.Get(apiId);
        set => Late.Set(ref apiId, value);
    }

    #endregion

    #region public late static string ApiHash { get; set; }

    static string? apiHash;

    public static string ApiHash {
        get => Late.Get(apiHash);
        set => Late.Set(ref apiHash, value);
    }

    #endregion

    #region public late static string SessionFilePath { get; set; }

    static string? sessionFilePath;

    public static string SessionFilePath {
        get => Late.Get(sessionFilePath);
        set => Late.Set(ref sessionFilePath, value);
    }

    #endregion

    #region public late static string Phone { get; set; }

    static string? phone;

    public static string Phone {
        get => Late.Get(phone);
        set => Late.Set(ref phone, value);
    }

    #endregion

    public static StorageClient Create(string spec) => throw new NotImplementedException();

    public override long Length(string path) {
        return 0;
    }

    public override void Delete(string path) {
        throw new NotImplementedException();
    }

    public override void Touch(string path) {
        throw new NotImplementedException();
    }

    public override Stream OpenRead(string path) => throw new NotImplementedException();

    public override void Write(string path, Stream stream) {
        throw new NotImplementedException();
    }

    public override string Type { get; }
    public override string Id { get; }
}
