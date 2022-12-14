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

    static void Work1(string id) {
        lock (GetLock(id)) {
            Console.WriteLine($"Start Work1: {id}");
            Thread.Sleep(TimeSpan.FromSeconds(10));
            Console.WriteLine($"End Work1: {id}");
        }
    }

    static void Work2(string id) {
        lock (GetLock(id)) {
            Console.WriteLine($"Start Work2: {id}");
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Console.WriteLine($"End Work2: {id}");
        }
    }

    public static void Main(string[] args) {
        var random = new Random();
        new List<string> {
            "abc",
            "bcd",
            "cde"
        }.SelectMany(id => new List<Action> {
            () => Work1(id),
            () => Work2(id)
        }).OrderBy(order => random.Next()).AsParallel().ForAll(f => f.Invoke());
    }
}
