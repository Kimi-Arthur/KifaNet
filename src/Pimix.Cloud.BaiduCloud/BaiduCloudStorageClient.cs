using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using NLog;
using Pimix.IO;

namespace Pimix.Cloud.BaiduCloud
{
    public class BaiduCloudStorageClient : StorageClient
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static BaiduCloudConfig Config { get; set; }

        public static StorageClient Get(string fileSpec)
        {
            var specs = fileSpec.Split(new char[] { ';' });
            foreach (var spec in specs)
            {
                if (spec.StartsWith("baidu:"))
                {
                    Config = BaiduCloudConfig.Get("default");
                    return new BaiduCloudStorageClient { AccountId = spec.Substring(6) };
                }
            }

            return null;
        }

        public override string ToString()
            => $"baidu:{AccountId}";

        private HttpClient Client;

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
            Client = new HttpClient();
        }

        int Download(byte[] buffer, string path, int bufferOffset = 0, long offset = 0, int count = -1)
        {
            if (count < 0)
            {
                count = buffer.Length - bufferOffset;
            }

            int step = 1 << 20;

            Parallel.For(0, (count - 1) / step + 1, i =>
            {
                DownloadSingleThread(buffer, path, bufferOffset + i * step, offset + i * step, Math.Min(step, count - step * i));
            });

            return count;
        }

        int DownloadSingleThread(byte[] buffer, string path, int bufferOffset, long offset, int count)
        {
            var request = GetRequest(Config.APIList.DownloadFile,
            new Dictionary<string, string>
            {
                ["remote_path"] = path.TrimStart('/')
            });

            request.Headers.Range = new RangeHeaderValue(offset, offset + count - 1);
            using (var response = Client.SendAsync(request).Result)
            {
                MemoryStream memoryStream = new MemoryStream(buffer, bufferOffset, count, true);
                response.Content.ReadAsStreamAsync().Result.CopyTo(memoryStream, count);
                return (int)memoryStream.Position;
            }
        }

        /// <summary>
        /// Upload data from stream with the optimal method.
        /// </summary>
        /// <param name="path">Path for the destination file.</param>
        /// <param name="stream">Input stream to upload.</param>
        public override void Write(string path, Stream stream)
        {
            UploadNormal(path, stream);
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

        void UploadNormal(string path, Stream input)
        {
            var size = input.Length;

            var blockSize = GetBlockSize(size);

            int blockLength = 0;
            byte[] buffer = new byte[blockSize];

            if (blockSize >= size)
            {
                blockLength = input.Read(buffer, 0, (int)size);
                bool uploadDirectDone = false;
                while (!uploadDirectDone)
                {
                    try
                    {
                        logger.Debug("Upload method: Direct");
                        UploadDirect(path, buffer, 0, blockLength);
                        uploadDirectDone = true;
                    }
                    catch (WebException ex)
                    {
                        logger.Warn($"WebException:\n{0}", ex);
                        if (ex.Response != null)
                        {
                            logger.Warn("Response:");
                            using (var s = new StreamReader(ex.Response.GetResponseStream()))
                            {
                                logger.Warn(s.ReadToEnd());
                            }
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    catch (ObjectDisposedException ex)
                    {
                        logger.Warn("Unexpected ObjectDisposedException:\n{0}", ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }
                return;
            }

            List<string> blockIds = new List<string>();

            logger.Debug("Upload method: Block");
            for (long position = 0; position < size; position += blockLength)
            {
                blockLength = input.Read(buffer, 0, blockSize);

                logger.Debug("Upload block ({0}): [{1}, {2})", position / blockSize, position, position + blockLength);

                bool done = false;
                while (!done)
                {
                    try
                    {
                        blockIds.Add(UploadBlock(buffer, 0, blockLength));
                        logger.Debug("Block ID/MD5: {0}", blockIds.Last());
                        done = true;
                    }
                    catch (WebException ex)
                    {
                        logger.Warn($"WebException:\n{0}", ex);
                        if (ex.Response != null)
                        {
                            logger.Warn("Response:");
                            using (var s = new StreamReader(ex.Response.GetResponseStream()))
                            {
                                logger.Warn(s.ReadToEnd());
                            }
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    catch (ObjectDisposedException ex)
                    {
                        logger.Warn("Unexpected ObjectDisposedException:\n{0}", ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                    catch (UploadBlockException ex)
                    {
                        logger.Warn("MD5 mismatch:\n{0}", ex);
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
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
                    logger.Warn(ex, "Failed when merging");
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

        public void UploadStreamRapid(string path, FileInformation fileInformation, Stream input = null)
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

        private HttpRequestMessage GetRequest(APIInfo api, Dictionary<string, string> parameters = null)
        {
            string address = api.Url.Format(parameters).Format(
                new Dictionary<string, string>
                {
                    ["access_token"] = Account.AccessToken,
                    ["remote_path_prefix"] = Config.RemotePathPrefix
                });

            logger.Trace($"{api.Method} {address}");
            return new HttpRequestMessage(new HttpMethod(api.Method), address);
        }

        private HttpWebRequest ConstructRequest(APIInfo api, Dictionary<string, string> parameters = null)
        {
            string address = api.Url.Format(parameters).Format(
                new Dictionary<string, string>
                {
                    ["access_token"] = Account.AccessToken,
                    ["remote_path_prefix"] = Config.RemotePathPrefix
                });

            logger.Trace("Constructed address: {0}", address);
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
            HttpWebRequest request = ConstructRequest(Config.APIList.CopyFile,
                new Dictionary<string, string>
                {
                    ["from_remote_path"] = sourcePath.TrimStart('/'),
                    ["to_remote_path"] = destinationPath.TrimStart('/')
                });
            try
            {
                using (var response = request.GetResponse())
                {
                    var value = response.GetJToken();
                    if (!((string)value["extra"]["list"][0]["from"]).EndsWith(sourcePath))
                        throw new Exception("from field is incorrect");
                    if (!((string)value["extra"]["list"][0]["to"]).EndsWith(destinationPath))
                        throw new Exception("to field is incorrect");
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Copy failed!");
                throw;
            }
        }

        public override void Move(string sourcePath, string destinationPath)
        {
            HttpWebRequest request = ConstructRequest(Config.APIList.MoveFile,
                new Dictionary<string, string>
                {
                    ["from_remote_path"] = sourcePath.TrimStart('/'),
                    ["to_remote_path"] = destinationPath.TrimStart('/')
                });
            try
            {
                using (var response = request.GetResponse())
                {
                    var value = response.GetJToken();
                    if (!((string)value["extra"]["list"][0]["from"]).EndsWith(sourcePath))
                        throw new Exception("from field is incorrect");
                    if (!((string)value["extra"]["list"][0]["to"]).EndsWith(destinationPath))
                        throw new Exception("to field is incorrect");
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Move failed!");
                throw;
            }
        }

        const long MaxBlockCount = 1L << 10;
        const long MaxBlockSize = 2L << 30;
        const long MinBlockSize = 32L << 20;

        static int GetBlockSize(long size)
        {
            long blockSize = MinBlockSize;

            // Special logic:
            //   1. Reserve one block for header
            //   2. Not stop when equals for the 'padding' logic

            while (blockSize <= MaxBlockSize && blockSize * (MaxBlockCount - 1) <= size)
            {
                blockSize <<= 1;
            }

            return (int)blockSize;
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
