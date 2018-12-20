using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using CommandLine;
using MongoDB.Driver;
using Newtonsoft.Json;
using Pimix.IO;

namespace Pimix.Apps.MongoUtil.Commands {
    [Verb("load", HelpText = "Load from json documents.")]
    public class LoadCommand {
        [Value(0)]
        public string Folder { get; set; }

        public int Execute() {
            var client = new MongoClient("mongodb://new.pimix.tk:27017");
            var db = client.GetDatabase("pimix");
            var collection = db.GetCollection<FileInformation>("files");

            var files = new List<FileInformation>();

            foreach (var file in new DirectoryInfo(Folder).GetFiles("*", SearchOption.AllDirectories)) {
                using (var sr = new StreamReader(file.Open(FileMode.Open))) {
                    files.Add(JsonConvert.DeserializeObject<FileInformation>(sr.ReadToEnd()));
                }
            }
            
            collection.InsertMany(files);

            return 0;
        }
    }
}
