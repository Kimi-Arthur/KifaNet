using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Pimix.Api.Files;
using Pimix.Bilibili;
using Pimix.IO;

namespace Pimix.Apps.BiliUtil {
    public static class Helper {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly Regex fileNamePattern = new Regex(@"^AV(\d+) P(\d+) .* cid (\d+)$");

        public static (string aid, int pid, string cid) GetIds(string name) {
            var match = fileNamePattern.Match(name);
            if (!match.Success) {
                return (null, 0, null);
            }

            return ($"av{match.Groups[1].Value}", int.Parse(match.Groups[2].Value),
                match.Groups[3].Value);
        }

        public static string GetDesiredFileName(BilibiliVideo video, int pid, string cid = null) {
            var p = video.Pages.First(x => x.Id == pid);

            if (cid != null && cid != p.Cid) {
                return null;
            }

            return video.Pages.Count > 1
                ? $"{video.Author}-{video.AuthorId}/{video.Title} P{pid} {p.Title}-{video.Id}p{pid}.c{cid}"
                : $"{video.Author}-{video.AuthorId}/{video.Title} {p.Title}-{video.Id}.c{cid}";
        }

        public static void WriteIfNotFinished(this PimixFile file, Func<Stream> getStream) {
            if (file.FileInfo.Locations != null) {
                logger.Info($"{file.FileInfo.Id} already exists in the system. Skipped.");
                return;
            }

            if (file.Exists()) {
                logger.Info($"Target file {file} already exists. Skipped.");
                return;
            }

            var downloadFile = file.GetFileSuffixed(".downloading");

            var stream = getStream();
            if (stream == null || stream.Length <= 0) {
                throw new Exception("Cannot get stream.");
            }

            if (downloadFile.Exists()) {
                if (downloadFile.Length() == stream.Length) {
                    downloadFile.Move(file);
                    logger.Info($"Moved {downloadFile} to {file} already exists. Skipped.");
                    return;
                }

                logger.Info($"Target file {downloadFile} exists, " +
                            $"but size ({downloadFile.Length()}) is different from source ({stream.Length}). " +
                            "Will be removed.");
            }

            logger.Info($"Start downloading video to {downloadFile}");
            downloadFile.Delete();
            downloadFile.Write(stream);
            downloadFile.Move(file);
            logger.Info($"Successfullly downloaded video to {file}");
        }


        public static void DownloadPart(this BilibiliVideo video, int pid, int sourceChoice, PimixFile currentFolder,
            string extraPath = null, bool prefixDate = false) {
            var (extension, streamGetters) = video.GetVideoStreams(pid, sourceChoice);
            if (extension == null) {
                return;
            }

            if (extension != "mp4") {
                var prefix = $"{video.GetDesiredName(pid, extraPath: extraPath, prefixDate: prefixDate)}";
                var finalTargetFile = currentFolder.GetFile($"{prefix}.mp4");
                if (finalTargetFile.Exists()) {
                    return;
                }

                List<PimixFile> partFiles = new List<PimixFile>();
                for (int i = 0; i < streamGetters.Count; i++) {
                    var targetFile = currentFolder.GetFile($"{prefix}-{i + 1}.{extension}");
                    try {
                        targetFile.WriteIfNotFinished(streamGetters[i]);
                    } catch (Exception e) {
                        logger.Warn(e, $"Failed to download {targetFile}.");
                    }

                    partFiles.Add(targetFile);
                }

                MergePartFiles(partFiles, finalTargetFile);
                RemovePartFiles(partFiles);
            } else {
                var targetFile =
                    currentFolder.GetFile(
                        $"{video.GetDesiredName(pid, extraPath: extraPath, prefixDate: prefixDate)}.{extension}");
                try {
                    targetFile.WriteIfNotFinished(streamGetters.First());
                } catch (Exception e) {
                    logger.Warn(e, $"Failed to download {targetFile}.");
                }
            }
        }

        static void MergePartFiles(List<PimixFile> parts, PimixFile target) {
            var partPaths = parts.Select(p => ((FileStorageClient) p.Client).GetPath(p.Path)).ToList();
            var fileListPath = Path.GetTempFileName();
            File.WriteAllLines(fileListPath, partPaths.Select(p => $"file '{p}'"));

            var targetPath = ((FileStorageClient) target.Client).GetPath(target.Path);
            var arguments = $"-safe 0 -f concat -i {fileListPath} -c copy \"{targetPath}\"";
            logger.Debug($"Executing: ffmpeg {arguments}");
            using var proc = new Process {
                StartInfo = {
                    FileName = "ffmpeg",
                    Arguments = arguments
                }
            };
            proc.Start();
            proc.WaitForExit();
            if (proc.ExitCode != 0) {
                throw new Exception("Merging files failed.");
            }

            File.Delete(fileListPath);
        }

        static void RemovePartFiles(List<PimixFile> partFiles) {
            partFiles.ForEach(p => p.Delete());
        }
    }
}
