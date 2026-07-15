using System.Collections.Generic;
using Kifa.Languages.Kindle.Models;
using Microsoft.Data.Sqlite;

namespace Kifa.Languages.Kindle;

public static class KindleVocabReader {
    public static List<BookInfo> GetBooks(string dbPath) {
        var books = new List<BookInfo>();
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT id, asin, guid, lang, title, authors FROM BOOK_INFO";

        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            books.Add(new BookInfo {
                Id = reader.GetString(0),
                Asin = reader.IsDBNull(1) ? null : reader.GetString(1),
                Guid = reader.IsDBNull(2) ? null : reader.GetString(2),
                Lang = reader.IsDBNull(3) ? null : reader.GetString(3),
                Title = reader.IsDBNull(4) ? null : reader.GetString(4),
                Authors = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return books;
    }

    public static List<Lookup> GetLookups(string dbPath) {
        var lookups = new List<Lookup>();
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT id, word_key, book_key, dict_key, pos, usage, timestamp FROM LOOKUPS";

        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            lookups.Add(new Lookup {
                Id = reader.GetString(0),
                WordKey = reader.IsDBNull(1) ? null : reader.GetString(1),
                BookKey = reader.IsDBNull(2) ? null : reader.GetString(2),
                DictKey = reader.IsDBNull(3) ? null : reader.GetString(3),
                Pos = reader.IsDBNull(4) ? null : reader.GetString(4),
                Usage = reader.IsDBNull(5) ? null : reader.GetString(5),
                Timestamp = reader.IsDBNull(6) ? 0 : reader.GetInt64(6)
            });
        }

        return lookups;
    }
}
