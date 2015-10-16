using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pimix.IO;

namespace Pimix.Cloud.BaiduCloud
{
    public class BaiduCloudStorageClient : StorageClient
    {
        private List<Stream> Streams { get; set; } = new List<Stream>();

        public static BaiduCloudConfig Config { get; set; }

        public List<int> BlockInfo { get; set; } = null;

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

        int Download(byte[] buffer, string path, int bufferOffset = 0, long offset = 0, int count = -1)
        {
            // All data contracts are checked by public methods.
            // All data here are expected to be correct.

            HttpWebRequest request = ConstructRequest(Config.APIList.DownloadFile,
                new Dictionary<string, string>
                {
                    ["remote_path"] = path.TrimStart('/')
                });
            request.Timeout = 30 * 60 * 1000;

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
        /// <param name="path">Path for the destination file.</param>
        /// <param name="stream">Input stream to upload.</param>
        /// <param name="fileInformation">
        /// If specified, contains information about the data to write.
        /// </param>
        /// <param name="match">Whether the fileInformation matches the stream.</param>
        public override void Write(string path, Stream stream = null, FileInformation fileInformation = null, bool match = true)
        {
            fileInformation = fileInformation ?? new FileInformation();
            if (match)
            {
                try
                {
                    UploadStreamRapid(path, stream, fileInformation);
                    return;
                }
                catch (Exception)
                {
                }
            }

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanSeek)
                throw new ArgumentException(
                    "Input stream needs to be seekable!",
                    nameof(stream));

            UploadNormal(path, stream, fileInformation);
        }

        public override void Delete(string path)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.RemovePath,
                new Dictionary<string, string>
                {
                    ["remote_path"] = path.TrimStart('/')
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

        void UploadNormal(string path, Stream input, FileInformation fileInformation)
        {
            fileInformation.AddProperties(input, FileProperties.Size | FileProperties.BlockSize);

            input.Seek(0, SeekOrigin.Begin);

            if (BlockInfo == null || BlockInfo.Count == 0)
            {
                BlockInfo = new List<int> { fileInformation.BlockSize.Value };
            }

            int blockIndex = 0;
            int blockLength = 0;
            byte[] buffer = new byte[BlockInfo.Max()];

            if (BlockInfo[0] >= fileInformation.Size)
            {
                blockLength = input.Read(buffer, 0, BlockInfo[0]);
                bool uploadDirectDone = false;
                while (!uploadDirectDone)
                {
                    try
                    {
                        UploadDirect(path, buffer, 0, blockLength);
                        uploadDirectDone = true;
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine($"Failed once when uploading file {path} with direct upload method.");
                        Console.WriteLine("Exception:");
                        Console.WriteLine(ex);
                        if (ex.Response != null)
                        {
                            Console.WriteLine("Response:");
                            using (var s = new StreamReader(ex.Response.GetResponseStream()))
                            {
                                Console.WriteLine(s.ReadToEnd());
                            }
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Console.WriteLine($"Failed once when uploading file {path} with direct upload method.");
                        Console.WriteLine("Unexpected ObjectDisposedException:");
                        Console.WriteLine(ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }
                return;
            }

            List<string> blockIds = new List<string>();

            for (long position = 0; position < fileInformation.Size; position += blockLength)
            {
                blockLength = input.Read(buffer, 0, BlockInfo[blockIndex]);

                bool done = false;
                while (!done)
                {
                    try
                    {
                        blockIds.Add(UploadBlock(buffer, 0, blockLength));
                        done = true;
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine($"Failed once for file {path}, on block {blockIds.Count}");
                        Console.WriteLine("Exception:");
                        Console.WriteLine(ex);
                        if (ex.Response != null)
                        {
                            Console.WriteLine("Response:");
                            using (var s = new StreamReader(ex.Response.GetResponseStream()))
                            {
                                Console.WriteLine(s.ReadToEnd());
                            }
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Console.WriteLine($"Failed once for file {path}, on block {blockIds.Count}");
                        Console.WriteLine("Unexpected ObjectDisposedException:");
                        Console.WriteLine(ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }

                if (blockIndex < BlockInfo.Count - 1)
                {
                    // Stay at the last element.
                    blockIndex++;
                }
            }

            bool mergeDone = false;
            while (!mergeDone)
            {
                try
                {
                    MergeBlocks(path, blockIds);
                    mergeDone = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed when merging");
                    Console.WriteLine("Exception:");
                    Console.WriteLine(ex);
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }
        }

        void UploadDirect(string path, byte[] buffer, int offset, int count)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.UploadFileDirect,
                new Dictionary<string, string>
                {
                    ["remote_path"] = path.TrimStart('/')
                });
            request.Timeout = 30 * 60 * 1000;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(buffer, offset, count);
            }

            using (var response = request.GetResponse())
            {
                var result = response.GetDictionary();
                if (!result["path"].ToString().EndsWith(path.TrimStart('/')))
                    throw new Exception($"Direct upload may fail: {path}, real path: {result["path"]}");
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

        void MergeBlocks(string path, List<string> blockList)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.MergeBlocks,
                new Dictionary<string, string>
                {
                    ["remote_path"] = path.TrimStart('/')
                });

            request.ContentType = "application/x-www-form-urlencoded";

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
                if (!result["path"].ToString().EndsWith(path))
                    throw new Exception($"Merge may fail! Original path: {path}, real path: {result["path"]}");
            }
        }

        void UploadStreamRapid(string path, Stream input, FileInformation fileInformation)
        {
            fileInformation.AddProperties(input, FileProperties.AllBaiduCloudRapidHashes);

            HttpWebRequest request = ConstructRequest(Config.APIList.UploadFileRapid,
                new Dictionary<string, string>
                {
                    ["remote_path"] = path.TrimStart('/'),
                    ["content_length"] = fileInformation.Size.ToString(),
                    ["content_md5"] = fileInformation.MD5,
                    ["slice_md5"] = fileInformation.SliceMD5,
                    ["content_crc32"] = fileInformation.Adler32
                });

            request.GetRequestStream().Close();

            using (var response = request.GetResponse())
            {
                if (!response.GetDictionary().Contains(new KeyValuePair<string, object>("md5", fileInformation.MD5)))
                    throw new Exception("Response is unexpected!");
            }
        }

        public long GetDownloadLength(string path)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.GetFileInfo,
                new Dictionary<string, string>
                {
                    ["remote_path"] = path.TrimStart('/')
                });

            using (var response = request.GetResponse())
            {
                return (long)response.GetJToken()["list"][0]["size"];
            }
        }

        public override Stream OpenRead(string path)
        {
            Streams.Add(new DownloadStream(this, path));
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
            //request.ReadWriteTimeout = 300000;
            request.Method = api.Method;

            return request;
        }

        public override bool Exists()
        {
            throw new NotImplementedException();
        }

        public override void Copy(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
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
                        Exception exception = null;
                        int retries = 5;
                        while (retries > 0)
                        {
                            try
                            {
                                length = Client.GetDownloadLength(Path);
                                retries = 0;
                                exception = null;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Failed when getting file length");
                                Console.WriteLine("Exception:");
                                Console.WriteLine(ex);
                                retries--;
                                exception = ex;
                                Thread.Sleep(TimeSpan.FromSeconds(10));
                            }
                        }

                        if (exception != null)
                            throw exception;
                    }

                    return length;
                }
            }

            public override long Position { get; set; }

            public BaiduCloudStorageClient Client { get; set; }

            public string Path { get; set; }

            public DownloadStream(BaiduCloudStorageClient client, string path)
            {
                Client = client;
                Path = path;
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

                count = (int)Math.Min(count, Length - Position);

                bool done = false;
                int readCount = 0;
                while (!done)
                {
                    try
                    {
                        readCount = Client.Download(buffer, Path, offset, Position, count);
                        done = readCount == count;
                        if (!done)
                        {
                            Console.Error.WriteLine("Didn't get expected amount of data.");
                            Console.Error.WriteLine($"Responses contains {readCount} bytes, should be {count} bytes.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed once when downloading:");
                        Console.WriteLine(ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
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
