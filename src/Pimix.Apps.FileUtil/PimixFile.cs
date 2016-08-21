using System.IO;
using Pimix.Cloud.BaiduCloud;
using Pimix.Cloud.MegaNz;
using Pimix.IO;
using Pimix.IO.FileFormats;

class PimixFile
{
    public StorageClient Client { get; set; }
    public string Path { get; set; }
    public PimixFileFormat FileFormat { get; set; }

    public PimixFile(string uri)
    {
        // Example uri (/files/ omitted):
        //   /files/v1;baidu:Pimix_1/a/b/c/d.txt
        //   /files/mega:0z/a/b/c/d.txt
        //   /files/local:cubie/a/b/c/d.txt
        //   /files/local:/a/b/c/d.txt
        //   /files//a/b/c/d.txt
        var segments = uri.Split(new char[] {'/'}, 2);
        Path = "/" + segments[1];

        var spec = string.IsNullOrEmpty(segments[0]) ? GetSpec(Path) : segments[0];
        Client = BaiduCloudStorageClient.Get(spec) ?? MegaNzStorageClient.Get(spec) ?? FileStorageClient.Get(spec);
        FileFormat = PimixFileV1Format.Get(spec) ?? PimixFileV0Format.Get(spec) ?? RawFileFormat.Get(spec);
        FileFormat.Info = FileInformation.Get(Path);
    }

    private string GetSpec(string Path)
    {
        var info = FileInformation.Get(Path);
        if (info == null)
        {
            return "";
        }

        foreach (var location in info.Locations)
        {
            // TODO: Will add selection logic here.
            return location.Key;
        }

        return "";
    }

    public void Exists()
        => Client.Exists(Path);

    public void Delete()
        => Client.Delete(Path);

    public void Copy(PimixFile destination)
    {
        if (destination.Client == Client && destination.FileFormat == FileFormat)
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
        if (destination.Client == Client && destination.FileFormat == FileFormat)
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
        => FileFormat.GetDecodeStream(Client.OpenRead(Path));

    public void Write(Stream stream = null, FileInformation fileInformation = null, bool match = true)
        => Client.Write(Path, FileFormat.GetEncodeStream(stream), fileInformation, match);
}
