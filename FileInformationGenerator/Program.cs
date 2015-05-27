using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.Service;
using Pimix.Storage;

namespace FileInformationGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                foreach (var path in arg.Split('\n'))
                {
                    string site = ConfigurationManager.AppSettings["Site"];
                    string pathPrefix = ConfigurationManager.AppSettings["PathPrefix"];
                    var info = FileUtility.GetInformation($"{pathPrefix}/{path}", FileProperties.All ^ FileProperties.Path);
                    info.Id = path;
                    info.Path = path;
                    info.Locations = new List<string> { $"{site}:{path}" };
                    DataModel.PimixServerApiAddress = ConfigurationManager.AppSettings["PimixServerApiAddress"];
                    DataModel.PimixServerCredential = ConfigurationManager.AppSettings["PimixServerCredential"];
                    DataModel.Patch(info);
                }
            }
        }
    }
}
