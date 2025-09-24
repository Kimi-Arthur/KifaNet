using System.Net.Http;
using Kifa.Rpc;

namespace Kifa.YouTube;

public sealed class ArchiveMetadataRpc : KifaJsonParameterizedRpc<ArchiveMetadataRpc.Response> {
    #region ArchiveMetadataRpc.Response

    public class Response {
        public long Created { get; set; }
        public string D1 { get; set; }
        public string D2 { get; set; }
        public string Dir { get; set; }
        public File[] Files { get; set; }
        public long FilesCount { get; set; }
        public long ItemLastUpdated { get; set; }
        public long ItemSize { get; set; }
        public Metadata Metadata { get; set; }
        public string Server { get; set; }
        public long Uniq { get; set; }
        public string[] WorkableServers { get; set; }
    }

    public class File {
        public string Name { get; set; }
        public string Source { get; set; }
        public string Mtime { get; set; }
        public string Size { get; set; }
        public string Md5 { get; set; }
        public string Crc32 { get; set; }
        public string Sha1 { get; set; }
        public string Format { get; set; }
        public string Rotation { get; set; }
        public string Length { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }
        public string Btih { get; set; }
        public string Summation { get; set; }
    }

    public class Metadata {
        public string Identifier { get; set; }
        public string[] Collection { get; set; }
        public string Creator { get; set; }
        public string Description { get; set; }
        public string Mediatype { get; set; }
        public string Subject { get; set; }
        public string Title { get; set; }
        public string Youtubechannel { get; set; }
        public string Youtubedisplayname { get; set; }
        public string Youtubeuser { get; set; }
        public string Publicdate { get; set; }
        public string Uploader { get; set; }
        public string Addeddate { get; set; }
        public string BackupLocation { get; set; }
        public string Noindex { get; set; }
    }

    #endregion

    protected override string Url => "https://archive.org/metadata/{id}";

    protected override HttpMethod Method => HttpMethod.Get;

    public ArchiveMetadataRpc(string id) {
        Parameters = new() {
            { "id", id }
        };
    }
}
