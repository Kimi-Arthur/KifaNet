using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        public void UploadStream(string remotePath, Stream input = null, bool tryRapid = true, List<int> blockInfo = null, FileInformation fileInformation = null)
        {
            fileInformation = fileInformation ?? new FileInformation();
            if (tryRapid)
            {
                try
                {
                    UploadStreamRapid(remotePath, input, fileInformation);
                    return;
                }
                catch (Exception)
                {
                }
            }

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (!input.CanSeek)
                throw new ArgumentException(
                    "Input stream needs to be seekable!",
                    nameof(input));

            UploadNormal(remotePath, input, blockInfo, fileInformation);
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

        void UploadNormal(string remotePath, Stream input, List<int> blockInfo, FileInformation fileInformation)
        {
            fileInformation.AddProperties(input, FileProperties.Size | FileProperties.BlockSize);

            input.Seek(0, SeekOrigin.Begin);

            if (blockInfo == null || blockInfo.Count == 0)
            {
                blockInfo = new List<int> { fileInformation.BlockSize.Value };
            }

            int blockIndex = 0;
            int blockLength = 0;
            byte[] buffer = new byte[blockInfo.Max()];

            if (blockInfo[0] >= fileInformation.Size)
            {
                blockLength = input.Read(buffer, 0, blockInfo[0]);
                UploadDirect(remotePath, buffer, 0, blockLength);
                return;
            }

            List<string> blockIds = new List<string>();

            for (int position = 0; position < fileInformation.Size; position += blockLength)
            {
                blockLength = input.Read(buffer, 0, blockInfo[blockIndex]);

                bool done = false;
                while (!done)
                {
                    try
                    {
                        blockIds.Add(UploadBlock(buffer, 0, blockLength));
                        done = true;
                    }
                    catch
                    {
                        Console.WriteLine($"Failed once for file {remotePath}, on block {blockIds.Count}");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }

                if (blockIndex < blockInfo.Count - 1)
                {
                    // Stay at the last element.
                    blockIndex++;
                }
            }

            MergeBlocks(remotePath, blockIds);
        }

        void UploadDirect(string remotePath, byte[] buffer, int offset, int count)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.UploadFileDirect,
                new Dictionary<string, string>
                {
                    ["remote_path"] = remotePath.TrimStart('/')
                });
            request.Timeout = 30 * 60 * 1000;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(buffer, offset, count);
            }

            using (var response = request.GetResponse())
            {
                var result = response.GetDictionary();
                if (!result["path"].ToString().EndsWith(remotePath.TrimStart('/')))
                    throw new Exception($"Direct upload may fail: {remotePath}, real path: {result["path"]}");
            }
        }

        string UploadBlock(byte[] buffer, int offset, int count)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.UploadBlock);
            request.Timeout = 30 * 60 * 1000;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(buffer, offset, count);
            }

            using (var response = request.GetResponse())
            {
                return response.GetDictionary()["md5"].ToString();
            }
        }

        void MergeBlocks(string remotePath, List<string> blockList)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.MergeBlocks,
                new Dictionary<string, string>
                {
                    ["remote_path"] = remotePath.TrimStart('/')
                });

            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30 * 60 * 1000;

            using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
            {
                sw.Write("param=");
                sw.Write(
                    Uri.EscapeDataString(
                        JsonConvert.SerializeObject(
                            new Dictionary<string, List<string>>
                            {
                                ["block_list"] = blockList
                            }
                        )
                    )
                );
            }

            using (var response = request.GetResponse())
            {
                var result = response.GetDictionary();
                if (!result["path"].ToString().EndsWith(remotePath))
                    throw new Exception($"Merge may fail! Original path: {remotePath}, real path: {result["path"]}");
            }
        }

        void UploadStreamRapid(string remotePath, Stream input, FileInformation fileInformation)
        {
            fileInformation.AddProperties(input, FileProperties.AllBaiduCloudRapidHashes);

            HttpWebRequest request = ConstructRequest(Config.APIList.UploadFileRapid,
                new Dictionary<string, string>
                {
                    ["remote_path"] = remotePath.TrimStart('/'),
                    ["content_length"] = fileInformation.Size.ToString(),
                    ["content_md5"] = fileInformation.MD5,
                    ["slice_md5"] = fileInformation.SliceMD5,
                    ["content_crc32"] = fileInformation.CRC32
                });

            request.GetRequestStream().Close();

            using (var response = request.GetResponse())
            {
                if (!response.GetDictionary().Contains(new KeyValuePair<string, object>("md5", fileInformation.MD5)))
                    throw new Exception("Response is unexpected!");
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

                if (Position >= Length)
                {
                    return 0;
                }

                bool done = false;
                int readCount = 0;
                while (!done)
                {
                    try
                    {
                        readCount = Client.Download(buffer, RemotePath, offset, Position, count);
                        done = true;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed once when downloading.");
                    }
                }

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
