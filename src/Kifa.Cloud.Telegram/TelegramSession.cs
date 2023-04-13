using System;

namespace Kifa.Cloud.Telegram;

public class TelegramSession {
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTimeOffset Reserved { get; set; } = Date.Zero;
}
