using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kifa.Api.Files;
using Kifa.Bilibili;
using Kifa.IO;
using NLog;

namespace Kifa.Tools.BiliUtil {
    public static class Helper {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static readonly Regex fileNamePattern = new Regex(@"^AV(\d+) P(\d+) .* cid (\d+)$");

        public static (string aid, int pid, string cid) GetIds(string name) {
            var match = fileNamePattern.Match(name);
            if (!match.Success) {
                return (null, 0, null);
            }

            return ($"av{match.Groups[1].Value}", int.Parse(match.Groups[2].Value), match.Groups[3].Value);
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

        public static void WriteIfNotFinished(this KifaFile file, Func<Stream> getStream) {
            if (file.Exists()) {
                logger.Info($"Target file {file} already exists. Skipped.");
                return;
            }

            var downloadFile = file.GetFileSuffixed(".downloading");

            var stream = getStream();
            if (stream == null || stream.Length <= 0) {
                throw new Exception("Cannot get stream.");
            }

            logger.Info($"Start downloading video to {downloadFile}");
            downloadFile.Write(stream);
            downloadFile.Move(file);
            logger.Info($"Successfullly downloaded video to {file}");
        }


        public static void DownloadPart(this BilibiliVideo video, int pid, int sourceChoice, KifaFile currentFolder,
            string extraPath = null, bool prefixDate = false, BilibiliUploader uploader = null) {
            uploader ??= new BilibiliUploader {
                Id = video.AuthorId,
                Name = video.Author
            };

            var (extension, quality, streamGetters) = video.GetVideoStreams(pid, sourceChoice);
            if (extension == null) {
                return;
            }

            if (extension != "mp4") {
                var prefix =
                    $"{video.GetDesiredName(pid, quality, extraPath: extraPath, prefixDate: prefixDate, uploader: uploader)}";
                var canonicalPrefix = video.GetCanonicalName(pid, quality);
                var canonicalTargetFile = currentFolder.GetFile($"{canonicalPrefix}.mp4");
                var finalTargetFile = currentFolder.GetFile($"{prefix}.mp4");
                if (finalTargetFile.ExistsSomewhere()) {
                    logger.Info($"{finalTargetFile.FileInfo.Id} already exists in the system. Skipped.");
                    if (!canonicalTargetFile.ExistsSomewhere()) {
                        FileInformation.Client.Link(finalTargetFile.Id, canonicalTargetFile.Id);
                        logger.Info($"Linked {canonicalTargetFile.Id} ==> {finalTargetFile.Id}");
                    }

                    return;
                }

                if (canonicalTargetFile.ExistsSomewhere()) {
                    logger.Info($"{canonicalTargetFile.FileInfo.Id} already exists in the system. Skipped.");
                    if (!finalTargetFile.ExistsSomewhere()) {
                        FileInformation.Client.Link(canonicalTargetFile.Id, finalTargetFile.Id);
                        logger.Info($"Linked {finalTargetFile.Id} ==> {canonicalTargetFile.Id}");
                    }

                    return;
                }

                if (canonicalTargetFile.Exists()) {
                    logger.Info($"Target file {finalTargetFile} already exists. Skipped.");
                    if (!finalTargetFile.Exists()) {
                        canonicalTargetFile.Copy(finalTargetFile);
                        logger.Info($"Linked {canonicalTargetFile} ==> {finalTargetFile}");
                    }

                    return;
                }

                if (finalTargetFile.Exists()) {
                    logger.Info($"Target file {finalTargetFile} already exists. Skipped.");
                    if (!canonicalTargetFile.Exists()) {
                        finalTargetFile.Copy(canonicalTargetFile);
                        logger.Info($"Linked {finalTargetFile} ==> {canonicalTargetFile}");
                    }

                    return;
                }

                List<KifaFile> partFiles = new List<KifaFile>();
                for (int i = 0; i < streamGetters.Count; i++) {
                    var targetFile = currentFolder.GetFile($"{canonicalPrefix}-{i + 1}.{extension}");
                    try {
                        targetFile.WriteIfNotFinished(streamGetters[i]);
                    } catch (Exception e) {
                        logger.Warn(e, $"Failed to download {targetFile}.");
                        return;
                    }

                    partFiles.Add(targetFile);
                }

                try {
                    MergePartFiles(partFiles, canonicalTargetFile);
                    RemovePartFiles(partFiles);
                    canonicalTargetFile.Copy(finalTargetFile);
                } catch (Exception e) {
                    logger.Warn(e, $"Failed to merge files.");
                }
            } else {
                var targetFile =
                    currentFolder.GetFile(
                        $"{video.GetDesiredName(pid, quality, extraPath: extraPath, prefixDate: prefixDate)}.{extension}");
                try {
                    targetFile.WriteIfNotFinished(streamGetters.First());
                } catch (Exception e) {
                    logger.Warn(e, $"Failed to download {targetFile}.");
                }
            }
        }

        public static void MergePartFiles(List<KifaFile> parts, KifaFile target) {
            // Convert parts first
            var partPaths = parts.Select(p => ConvertPartFile(((FileStorageClient) p.Client).GetPath(p.Path))).ToList();

            var fileListPath = Path.GetTempFileName();
            File.WriteAllLines(fileListPath, partPaths.Select(p => $"file {GeFfmpegTargetPath(p)}"));

            var targetPath = ((FileStorageClient) target.Client).GetPath(target.Path);
            var arguments = $"-safe 0 -f concat -i \"{fileListPath}\" -c copy \"{targetPath}\"";
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

            // Delete part files
            foreach (var partPath in partPaths) {
                File.Delete(partPath);
            }
        }

        static string ConvertPartFile(string path) {
            var newPath = Path.GetTempPath() + path.Split("/").Last() + ".mp4";
            var arguments = $"-i \"{path}\" -c copy \"{newPath}\"";
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
                throw new Exception("Converting part files failed.");
            }

            return newPath;
        }

        static string GeFfmpegTargetPath(string targetPath) {
            return string.Join("\\'", targetPath.Split("'").Select(s => $"'{s}'"));
        }

        static void RemovePartFiles(List<KifaFile> partFiles) {
            partFiles.ForEach(p => p.Delete());
        }
    }
}
