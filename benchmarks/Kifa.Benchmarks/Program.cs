using System;

namespace Kifa.Benchmarks;

class A {
    string b;

    public string B {
        get => b;
        set {
            Console.WriteLine("called");
            b = value;
        }
    }
}

class Program {
    static void Main(string[] args) {
        var x = new A();
        x.B = "d";
        Console.WriteLine(x.B);
    }
}
