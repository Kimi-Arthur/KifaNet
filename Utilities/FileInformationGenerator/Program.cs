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
            string site = "cubie";
            var info = FileUtility.GetInformation($"\\\\{site}.pimix.org/files/{path}", FileProperties.All ^ FileProperties.Path);
            info.Id = path;
            info.Path = path;
            info.Locations = new List<string> { $"{site}:{path}" };
            DataModel.PimixServerApiAddress = ConfigurationManager.AppSettings["PimixServerApiAddress"];
            DataModel.PimixServerCredential = ConfigurationManager.AppSettings["PimixServerCredential"];
            DataModel.Patch(info);
        }
    }
}
