using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace jobutil
{
    class Program
    {
        static int Main(string[] args)
            => Parser.Default.ParseArguments<RunJobCommand>(args)
            .Return(
                x => x.Execute(),
                x => 1);
    }
}
