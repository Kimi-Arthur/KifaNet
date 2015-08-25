using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HashLib;
using Newtonsoft.Json;
using Pimix.Service;

namespace Pimix.IO
{
    /// <summary>
    /// Service wrapper for FileInformation
    /// </summary>
    public partial class FileInformation
    {
        public static string PimixServerApiAddress
        {
            get
            {
                return PimixService.PimixServerApiAddress;
            }
            set
            {
                PimixService.PimixServerApiAddress = value;
            }
        }

        public static string PimixServerCredential
        {
            get
            {
                return PimixService.PimixServerCredential;
            }
            set
            {
                PimixService.PimixServerCredential = value;
            }
        }

        public static bool Patch(FileInformation data, string id = null)
            => PimixService.Patch(data, id);

        public static bool Post(FileInformation data, string id = null)
            => PimixService.Post(data, id);

        public static FileInformation Get(string id)
            => PimixService.Get<FileInformation>(id);

        public static bool Delete(string id)
            => PimixService.Delete<FileInformation>(id);

        public static List<FileInformation> GetFolderView(string path)
            => PimixService.Call<FileInformation, List<FileInformation>>("get_folder_view", parameters: new Dictionary<string, string> {["location"] = path });
    }
}
