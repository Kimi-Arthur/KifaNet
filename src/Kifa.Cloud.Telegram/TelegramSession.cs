using System;

namespace Kifa.Cloud.Telegram;

public class TelegramSession {
    public int Id { get; set; }

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTimeOffset Reserved { get; set; } = Date.Zero;
}
