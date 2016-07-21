
class Location
{
    public StorageClient Client { get; set; }
    public string Path { get; set; }
    public PimixFileFormat FileFormat { get; set; }

    public Location(string uri)
    {
    }

    public void Exists()
        => Client.Exists(Path);

    public void Delete()
        => Client.Delete(Path);

    public void Copy(Location destination)
    {
        if (destination.Client == Client)
        {
            Client.Copy(Path, destination.Path);
        }
        else
        {
            destination.Write(OpenRead());
        }
    }

    public void Move(Location destination)
    {
        if (destination.Client == Client)
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
        => Client.Write(FileFormat.GetEncodeStream(stream), fileInformation, match);
}