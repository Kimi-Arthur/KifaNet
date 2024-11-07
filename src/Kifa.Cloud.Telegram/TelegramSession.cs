using System;

namespace Kifa.Cloud.Telegram;

public class TelegramSession {
    public int Id { get; set; }

    public byte[] Data { get; set; } = [];

    public DateTimeOffset Reserved { get; set; } = Date.Zero;

    public DateTimeOffset Refreshed { get; set; } = Date.Zero;
}
