using System.IO;
using Pimix.IO;
using Pimix.IO.FileFormats;

class PimixFile
{
    public StorageClient Client { get; set; }
    public string Path { get; set; }
    public PimixFileFormat FileFormat { get; set; }

    public PimixFile(string uri)
    {
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