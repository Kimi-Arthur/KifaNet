using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pimix.Cloud.BaiduCloud;
using Pimix.Cloud.MegaNz;
using Pimix.IO;
using Pimix.IO.FileFormats;

class PimixFile
{
    public string Id { get; set; }

    public string Path { get; set; }

    public string Spec
        => string.Join(";", new string[]
        {
            Client.ToString(),
            FileFormat.ToString()
        }.Where((x) => x != null));

    public StorageClient Client { get; set; }

    public PimixFileFormat FileFormat { get; set; }

    public FileInformation FileInfo => FileInformation.Get(Id);


    public PimixFile(string uri, string id = null)
    {
        // Example uri:
        //   baidu:Pimix_1;v1/a/b/c/d.txt
        //   mega:0z/a/b/c/d.txt
        //   local:cubie/a/b/c/d.txt
        //   local:/a/b/c/d.txt
        //   /a/b/c/d.txt
        var segments = uri.Split(new char[] { '/' }, 2);
        Path = "/" + segments[1];
        Id = id ?? Path;

        var spec = string.IsNullOrEmpty(segments[0]) ? GetSpec(Path) : segments[0];

        Client = BaiduCloudStorageClient.Get(spec) ?? MegaNzStorageClient.Get(spec) ?? FileStorageClient.Get(spec);

        FileFormat = PimixFileV1Format.Get(spec) ?? PimixFileV0Format.Get(spec) ?? RawFileFormat.Get(spec);
    }

    public override string ToString()
        => $"{Spec}{Path}";

    string GetSpec(string Path)
    {
        var info = FileInformation.Get(Path);
        if (info == null)
        {
            return "";
        }

        foreach (var location in info.Locations)
        {
            // TODO: Will add selection logic here.
            return location;
        }

        return "";
    }

    public void Exists()
        => Client.Exists(Path);

    public void Delete()
        => Client.Delete(Path);

    public void Copy(PimixFile destination)
    {
        if (Spec == destination.Spec)
        {
            Client.Copy(Path, destination.Path);
        }
        else
        {
            destination.Write(OpenRead());
        }
    }

    public void Move(PimixFile destination)
    {
        if (Spec == destination.Spec)
        {
            Client.Move(Path, destination.Path);
        }
        else
        {
            Copy(destination);
            Delete();
        }
    }

    public Stream OpenRead()
        => FileFormat.GetDecodeStream(Client.OpenRead(Path), FileInfo.EncryptionKey);

    public void Write(Stream stream)
        => Client.Write(Path, FileFormat.GetEncodeStream(stream, FileInfo));

    public FileInformation CalculateInfo(FileProperties properties)
    {
        var info = FileInfo;
        info.RemoveProperties(FileProperties.AllVerifiable & properties);

        using (var stream = OpenRead())
        {
            info.AddProperties(stream, properties);
        }

        return info;
    }
}
