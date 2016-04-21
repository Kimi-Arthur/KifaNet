using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using Pimix.IO;

namespace Pimix.Cloud.BaiduCloud
{
    public class BaiduCloudStorageClient : StorageClient
    {
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Rapid upload failed!\nException:\n{ex}\n");
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
                        Console.WriteLine("Exception:");
                        Console.WriteLine(ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    catch (UploadBlockException ex)
                    {
                        Console.WriteLine($"Failed once for file {path}, on block {blockIds.Count}");
                        Console.WriteLine("Exception:");
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
            string expectedMd5 = new MD5CryptoServiceProvider().ComputeHash(buffer, offset, count).ToHexString().ToLower();
            HttpWebRequest request = ConstructRequest(Config.APIList.UploadBlock);
            request.Timeout = 30 * 60 * 1000;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(buffer, offset, count);
            }

            using (var response = request.GetResponse())
            {
                string actualMd5 = response.GetDictionary()["md5"].ToString();
                if (expectedMd5 != actualMd5)
                    throw new UploadBlockException() { ExpectedMd5 = expectedMd5, ActualMd5 = actualMd5 };
                return actualMd5;
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


        static RetryPolicy defaultRetryPolicy;
        public static RetryPolicy DefaultRetryPolicy
        {
            get
            {
                if (defaultRetryPolicy == null)
                {
                    defaultRetryPolicy = new RetryPolicy<BaiduCloudTransientErrorDetectionStrategy>(5, TimeSpan.FromSeconds(10));
                    defaultRetryPolicy.Retrying += (sender, args) =>
                    {
                        Console.Error.WriteLine("Get download length failed once, wait 10 seconds now!");
                        Console.Error.WriteLine(args.LastException);
                    };
                }

                return defaultRetryPolicy;
            }
            set
            {
                defaultRetryPolicy = value;
            }
        }

        public long GetDownloadLength(string path)
            => DefaultRetryPolicy.ExecuteAction(() => GetDownloadLengthWithoutRetry(path));

        public long GetDownloadLengthWithoutRetry(string path)
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
            => new SeekableDownloadStream(GetDownloadLength(path), (buffer, bufferOffset, offset, count) => Download(buffer, path, bufferOffset, offset, count));

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

        public override bool Exists(string path)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.GetFileInfo,
                new Dictionary<string, string>
                {
                    ["remote_path"] = path.TrimStart('/')
                });

            try
            {
                using (var response = request.GetResponse())
                {
                    return (long)response.GetJToken()["list"][0]["size"] > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void Copy(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        class UploadBlockException : Exception
        {
            public string ExpectedMd5 { get; set; }

            public string ActualMd5 { get; set; }

            public override string ToString()
                => $"Expected md5 is {ExpectedMd5}, while actual md5 is {ActualMd5}.";
        }
    }
}
