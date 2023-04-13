using System;

namespace Kifa.Cloud.Telegram;

public class TelegramSession {
    #region public late string Id { get; set; }

    string? id;

    public string Id {
        get => Late.Get(id);
        set => Late.Set(ref id, value);
    }

    #endregion

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTimeOffset Reserved { get; set; } = Date.Zero;
}
