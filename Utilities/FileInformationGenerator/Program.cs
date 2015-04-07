using Newtonsoft.Json;
using Pimix.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileInformationGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var info = FileUtility.GetInformation(args[0], FileProperties.All);
            Console.WriteLine(JsonConvert.SerializeObject(info));
        }
    }
}
