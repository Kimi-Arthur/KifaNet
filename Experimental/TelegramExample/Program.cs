using System;
using System.Linq;
using System.Threading;
using TL;
using WTelegram;

var apiId = 0;
var apiHash = "<api_hash>";
var sessionFile = "<session_file>";
var phoneNumber = "+<phone>";
var chatId = 0;

var client = new Client(apiId, apiHash, sessionFile);

Console.WriteLine($"Login: {client.Login(phoneNumber).Result}");

var channel = client.Messages_GetAllChats().Result.chats[chatId];

var text = "whywhywhy";

for (int i = 0; i < 100000; i++) {
    var history = client.Messages_GetHistory(channel).Result.Messages.Select(m => m as Message)
        .Where(m => m?.message == text).ToList();
    foreach (var m in history) {
        Console.WriteLine($"History: {m.Date}, {m.ID}");
    }

    Console.WriteLine($"History (should be {i}): {history.Count}");

    Console.WriteLine(
        $"Search (should be {i}): {client.Messages_Search(channel, text).Result.Count}");

    Console.WriteLine($"Send: {client.SendMessageAsync(channel, text).Result}");
    Thread.Sleep(TimeSpan.FromSeconds(10));
}
