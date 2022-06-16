using System;
using System.Collections.Generic;
using System.Linq;

namespace Kifa.Benchmarks;

class Test {
    string value;

    public string Value {
        get {
            Console.WriteLine("get");
            return value;
        }
        set {
            Console.WriteLine("set");
            this.value = value;
        }
    }
}

class Program {
    static void Main(string[] args) {
        var x = new Test();
        x.Value ??= "abc";
    }
}
