using Newtonsoft.Json;
using Pimix.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pimix.Service;
using System.Configuration;

namespace FileInformationGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = args[0];
            string site = "mac";
            var info = FileUtility.GetInformation($"\\\\{site}.pimix.org/files/{path}", FileProperties.All ^ FileProperties.Path);
            info.Id = path;
            info.Path = path;
            info.Locations = new List<string> { $"{site}:{path}" };
            Console.WriteLine(JsonConvert.SerializeObject(info));
            DataModel.PimixServerApiAddress = ConfigurationManager.AppSettings["PimixServerApiAddress"];
            DataModel.PimixServerCredential = ConfigurationManager.AppSettings["PimixServerCredential"];
            DataModel.Patch(info);
        }
    }
}
