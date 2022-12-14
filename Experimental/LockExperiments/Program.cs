// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using Kifa.Service;

namespace MyNamespace;

class TestModel : DataModel {
    public string? Value { get; set; }
}

public class Program {
    static ConcurrentDictionary<string, Link<TestModel>> Locks = new();

    static Link<TestModel> GetLock(string id) => Locks.GetOrAdd(id, key => key);
    static void DeleteLock(string id) => Locks.Remove(id, out _);

    static void Work1(string id) {
        lock (GetLock(id)) {
            Console.WriteLine($"Start Work1 ({DateTime.Now}): {id}");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.WriteLine($"End Work1 ({DateTime.Now}): {id}");
        }

        DeleteLock(id);
    }

    static void Work2(string id) {
        lock (GetLock(id)) {
            Console.WriteLine($"Start Work2 ({DateTime.Now}): {id}");
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
            Console.WriteLine($"End Work2 ({DateTime.Now}): {id}");
        }

        DeleteLock(id);
    }

    static void Try1() {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        Enumerable.Repeat("", 10000).Select<string, string>(_
                => new string(Enumerable.Repeat(chars, 100).Select(s => s[random.Next(s.Length)])
                    .ToArray())).SelectMany(id => new List<Action> {
                () => Work1(id),
                () => Work2(id)
            }).OrderBy(order => random.Next()).AsParallel().WithDegreeOfParallelism(100)
            .ForAll(f => f.Invoke());
    }

    static void Try2() {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        Enumerable.Repeat("", 10000).Select<string, string>(_
                => new string(Enumerable.Repeat(chars, 100).Select(s => s[random.Next(s.Length)])
                    .ToArray())).SelectMany(id => new List<Action> {
                () => Work1(id),
                () => Work2(id)
            }).OrderBy(order => random.Next()).AsParallel().WithDegreeOfParallelism(100)
            .ForAll(f => f.Invoke());
    }

    public static void Main(string[] args) {
        Try1();
        Console.ReadLine();
        Try2();
    }
}
