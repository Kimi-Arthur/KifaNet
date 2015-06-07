using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Pimix.Storage;

namespace Pimix.Cloud.BaiduCloud
{
    public class BaiduCloudStorageClient
    {
        private List<Stream> Streams { get; set; } = new List<Stream>();

        public static BaiduCloudConfig Config { get; set; }

        string accountId;
        public string AccountId
        {
            get
            {
                return accountId;
            }
            set
            {
                accountId = value;
                Account = Config.Accounts[accountId];

                // Clear all current streams since they are all stale.
                foreach (var stream in Streams)
                {
                    stream.Dispose();
                }

                Streams.Clear();
            }
        }

        public AccountInfo Account { get; private set; }

        public BaiduCloudStorageClient()
        {

        }

        int Download(byte[] buffer, string remotePath, int bufferOffset = 0, long offset = 0, int count = -1)
        {
            // All data contracts are checked by public methods.
            // All data here are expected to be correct.

            HttpWebRequest request = ConstructRequest(Config.APIList.DownloadFile,
                new Dictionary<string, string>
                {
                    ["remote_path"] = remotePath.TrimStart('/')
                });

            if (count < 0)
            {
                count = buffer.Length - bufferOffset;
            }

            request.AddRange(offset, offset + count - 1);

            using (var response = request.GetResponse())
            {
                MemoryStream memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
                response.GetResponseStream().CopyTo(memoryStream, count);
                return (int)memoryStream.Position;
            }
        }

        /// <summary>
        /// Upload data from stream with the optimal method.
        /// </summary>
        /// <param name="remotePath">Remote path for the uploaded file.</param>
        /// <param name="input">Input stream to upload.</param>
        /// <param name="tryRapid">If rapid upload is tried before normal upload.</param>
        /// <param name="blockInfo">Contains block info as a series of lengths.</param>
        /// <param name="fileInformation">
        /// If specified, contains information about the file to be uploaded.
        /// </param>
        public void UploadStream(string remotePath, Stream input = null, bool tryRapid = true, IEnumerable<int> blockInfo = null, FileInformation fileInformation = null)
        {
            fileInformation = fileInformation ?? new FileInformation();
            if (tryRapid)
            {
                if (UploadStreamRapid(remotePath, input, fileInformation))
                {
                    return;
                }
            }

            UploadStreamByBlock(remotePath, input, blockInfo, fileInformation);
        }

        public void DeleteFile(string remotePath)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.RemovePath,
                new Dictionary<string, string>
                {
                    ["remote_path"] = remotePath.TrimStart('/')
                });

            request.GetRequestStream().Close();

            using (var response = request.GetResponse())
            {
                if (response.GetDictionary() == null)
                {
                    // TODO: Add appropriate exceptions.
                    throw new InvalidOperationException();
                }
            }
        }

        void UploadStreamByBlock(string remotePath, Stream input, IEnumerable<int> blockInfo, FileInformation fileInformation)
        {
            FileProperties properties = FileProperties.Size;
            properties -= properties & fileInformation.GetProperties();
        }

        bool UploadStreamRapid(string remotePath, Stream input, FileInformation fileInformation)
        {
            FileProperties properties = FileProperties.AllBaiduCloudRapidHashes;
            properties -= properties & fileInformation.GetProperties();

            FileInformation calculatedInfo = FileUtility.GetInformation(input, properties);

            HttpWebRequest request = ConstructRequest(Config.APIList.UploadFileRapid,
                new Dictionary<string, string>
                {
                    ["remote_path"] = remotePath.TrimStart('/'),
                    ["content_length"] = (fileInformation.Size ?? calculatedInfo.Size).ToString(),
                    ["content_md5"] = fileInformation.MD5 ?? calculatedInfo.MD5,
                    ["slice_md5"] = fileInformation.SliceMD5 ?? calculatedInfo.SliceMD5,
                    ["content_crc32"] = fileInformation.CRC32 ?? calculatedInfo.CRC32
                });

            request.GetRequestStream().Close();

            using (var response = request.GetResponse())
            {
                return response.GetDictionary().Contains(new KeyValuePair<string, object>("md5", fileInformation.MD5 ?? calculatedInfo.MD5));
            }
        }

        public long GetDownloadLength(string remotePath)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.DownloadFile,
                new Dictionary<string, string>
                {
                    ["remote_path"] = remotePath.TrimStart('/')
                });
            request.Method = "HEAD";

            using (var response = request.GetResponse())
            {
                return response.ContentLength;
            }
        }

        public Stream GetDownloadStream(string remotePath)
        {
            Streams.Add(new DownloadStream(this, remotePath));
            return Streams.Last();
        }

        private HttpWebRequest ConstructRequest(APIInfo api, Dictionary<string, string> parameters = null)
        {
            string address = api.Url.Format(parameters).Format(
                new Dictionary<string, string>
                {
                    ["access_token"] = Account.AccessToken,
                    ["remote_path_prefix"] = Config.RemotePathPrefix
                });

            HttpWebRequest request = WebRequest.CreateHttp(address);
            request.Method = api.Method;

            return request;
        }

        private class DownloadStream : Stream
        {
            private bool IsOpen = true;

            public override bool CanRead
                => IsOpen;

            public override bool CanSeek
                => IsOpen;

            public override bool CanWrite
                => false;

            long length = -1;
            public override long Length
            {
                get
                {
                    if (!IsOpen)
                        throw new ObjectDisposedException(null);

                    if (length < 0)
                    {
                        length = Client.GetDownloadLength(RemotePath);
                    }

                    return length;
                }
            }

            public override long Position { get; set; }

            public BaiduCloudStorageClient Client { get; set; }

            public string RemotePath { get; set; }

            public DownloadStream(BaiduCloudStorageClient client, string remotePath)
            {
                Client = client;
                RemotePath = remotePath;
            }

            public override void Flush()
            {
                if (!IsOpen)
                    throw new ObjectDisposedException(null);

                // Intentionally doing nothing.
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (!IsOpen)
                    throw new ObjectDisposedException(null);

                switch (origin)
                {
                    case SeekOrigin.Begin:
                        Position = offset;
                        break;
                    case SeekOrigin.Current:
                        Position += offset;
                        break;
                    case SeekOrigin.End:
                        Position = Length + offset;
                        break;
                }

                return Position;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException("The Baidu download stream is not writable.");
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!IsOpen)
                    throw new ObjectDisposedException(null);

                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));

                if (buffer.Length - offset < count)
                    throw new ArgumentException();


                int readCount = Client.Download(buffer, RemotePath, offset, Position, count);
                Position += readCount;

                return readCount;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("The Baidu download stream is not writable.");
            }
        }
    }
}
