using System;
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

// Using InputPeer.Self will make the last line pass even if the breaking line is not commented.
// var channel = InputPeer.Self;

// var text = Random.Shared.NextInt64().ToString();
var text = "abcdefghijklmn";

// Commenting this line will make the last line pass. (breaking line)
Console.WriteLine(
    $"Before (should be 0): {client.Messages_Search(channel, text, min_date: DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(6))).Result.Count}");

foreach (var m in client.Messages_GetHistory(channel).Result.Messages) {
    Console.WriteLine($"History: {m.Date}");
}

Console.WriteLine($"Send: {client.SendMessageAsync(channel, text).Result}");

Console.WriteLine($"After (should be 1): {client.Messages_Search(channel, text).Result.Count}");

foreach (var m in client.Messages_GetHistory(channel).Result.Messages) {
    Console.WriteLine($"History: {m.Date}");
}
