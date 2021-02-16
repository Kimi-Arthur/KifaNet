using System;
using System.Collections.Generic;
using System.Linq;

namespace Kifa.Benchmarks {
    class Program {
        static void Main(string[] args) {
            var random = new Random();
            var ints = new List<int>();
            for (var i = 0; i < 500000000; i++) {
                ints.Add(random.Next(2));
            }

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));

            var sum1 = 0;
            for (var i = 10000000; i < 500000000; i++) {
                sum1 += ints[i];
            }

            Console.WriteLine(sum1);

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));

            var sum2 = 0;
            for (var i = 10000000; i < ints.Count; i++) {
                sum2 += ints[i];
            }

            Console.WriteLine(sum2);

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));

            var sum3 = ints.Skip(10000000).Sum();

            Console.WriteLine(sum3);

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));

            var sum4 = 0;

            foreach (var i in ints.Skip(10000000)) {
                sum4 += i;
            }

            Console.WriteLine(sum4);

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));

            var sum5 = 0;

            foreach (var i in ints) {
                sum5 += i;
            }

            Console.WriteLine(sum5);

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));

            var sum6 = ints.Sum();

            Console.WriteLine(sum6);

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"));
        }
    }
}
