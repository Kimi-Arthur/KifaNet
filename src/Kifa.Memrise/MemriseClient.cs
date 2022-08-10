using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Kifa.Api.Files;
using Kifa.Languages.German;
using Kifa.Languages.German.Goethe;
using Kifa.Memrise.Api;
using Kifa.Service;
using NLog;

namespace Kifa.Memrise;

public class MemriseClient : IDisposable {
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static readonly string LineBreak = new(' ', 100);

    public static string WebDriverUrl { get; set; }
    public static string Cookies { get; set; }
    public static string CsrfToken { get; set; }

    public MemriseCourse Course { get; init; }

    HttpClient? httpClient;

    HttpClient HttpClient {
        get {
            if (httpClient == null) {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("cookie", Cookies);
                httpClient.DefaultRequestHeaders.Add("x-csrftoken", CsrfToken);
                httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
                httpClient.DefaultRequestHeaders.Add("referer", Course.BaseUrl);
            }

            return httpClient;
        }
    }

    GoetheGermanWordRestServiceClient GoetheClient = new();

    GermanWordServiceClient WordClient => GermanWord.Client;

    MemriseCourseServiceClient CourseClient => MemriseCourse.Client;

    public KifaActionResult AddWordList(GoetheWordList wordList) {
        AddWordsToLevel(Course.Levels[wordList.Id], AddWords(ExpandWords(wordList.Words)).ToList());

        return KifaActionResult.Success;
    }

    public IEnumerable<GoetheGermanWord> ExpandWords(IEnumerable<string> words) {
        foreach (var word in words) {
            var expandedWords = new Queue<GoetheGermanWord>();
            var originalWord = GoetheClient.Get(word);
            if (originalWord == null) {
                throw new NullReferenceException($"{nameof(originalWord)} is null.");
            }

            expandedWords.Enqueue(originalWord);
            while (expandedWords.Count > 0) {
                var goetheWord = expandedWords.Dequeue();
                if (goetheWord.Examples?[0].StartsWith("example") == true) {
                    goetheWord.Examples = null;
                }

                yield return goetheWord;

                if (goetheWord.Feminine != null) {
                    var feminineWord = goetheWord.Feminine;
                    feminineWord.Meaning = $"(female) {goetheWord.Meaning}";
                    expandedWords.Enqueue(feminineWord);
                }

                if (goetheWord.Abbreviation != null) {
                    var abbrWord = goetheWord.Abbreviation;
                    abbrWord.Meaning = $"{goetheWord.Meaning} (abbr)";
                    expandedWords.Enqueue(abbrWord);
                }
            }
        }
    }

    IEnumerable<string> AddWords(IEnumerable<GoetheGermanWord> words) {
        foreach (var word in words) {
            var addedWord = AddWord(word);
            Logger.LogResult(addedWord, $"Upload word {word}");
            if (addedWord.Status == KifaActionStatus.OK) {
                var added = addedWord.Response!;
                Logger.Debug($"Update Memrise record: {added}.");
                CourseClient.AddWord(Course.Id, added);
                yield return added.Id;
            }
        }
    }

    void AddWordsToLevel(string levelId, List<string> wordIds) {
        var rendered = new GetLevelRpc {
            HttpClient = HttpClient
        }.Invoke(Course.DatabaseUrl, levelId)?.Rendered;
        if (rendered == null) {
            throw new Exception($"Failed to get current words in level {levelId}.");
        }

        var thingIdReg = new Regex(@"data-thing-id=""(\d+)""");
        var existingThingIds =
            thingIdReg.Matches(rendered).Select(m => m.Groups[1].Value).ToHashSet();

        foreach (var wordId in wordIds.Except(existingThingIds)) {
            Logger.Debug(
                $"Add word {wordId} to level {levelId}: {new AddWordToLevelRpc { HttpClient = HttpClient }.Invoke(Course.DatabaseUrl, levelId, wordId)?.Success}");
        }

        foreach (var wordId in existingThingIds.Except(wordIds)) {
            Logger.Debug(
                $"Remove word {wordId} from level {levelId}: {new RemoveWordFromLevelRpc { HttpClient = HttpClient }.Invoke(Course.DatabaseUrl, levelId, wordId)?.Success}");
        }

        Logger.Debug(
            $"Reorder words for {levelId}: {new ReorderWordsInLevelRpc { HttpClient = HttpClient }.Invoke(Course.DatabaseUrl, levelId, wordIds)?.Success}");
    }

    public KifaActionResult<MemriseWord> AddWord(GoetheGermanWord word) {
        var rootWord = WordClient.Get(word.RootWord);
        if (rootWord == null) {
            return new KifaActionResult<MemriseWord> {
                Message = "Failed to get root word.",
                Status = KifaActionStatus.Error
            };
        }

        Logger.Info($"{word.Id} => {rootWord.Id}");

        Logger.Debug($"Adding word in {Course.DatabaseUrl}:\n{word}\n{rootWord}");

        var newData = GetDataFromWord(word, rootWord);

        var existingRow = Course.Words.GetValueOrDefault(word.Id)?.Data ?? GetExistingRow(word);

        if (existingRow == null) {
            var thingId = Retry.Run(() => FillBasicWord(newData), (ex, index) => {
                if (index > 5) {
                    throw ex;
                }

                Logger.Warn($"Failed to fill basic row. Retrying ({index + 1}).");
            }, (result, index) => {
                if (result != null) {
                    return true;
                }

                if (index > 5) {
                    throw new Exception("Failed to fill basic row.");
                }

                Logger.Warn($"Retry result ({result}) is not valid. Retrying ({index + 1}).");
                return false;
            });

            Thread.Sleep(TimeSpan.FromSeconds(5));
            existingRow = GetExistingRow(word);
            if (existingRow == null) {
                Logger.Error($"Failed to get filled row of id {thingId}.");
                return new KifaActionResult<MemriseWord>(KifaActionStatus.Error,
                    $"failed to add word {word.Id}");
            }

            if (existingRow.Id != thingId) {
                Logger.Error($"Thing ids ({existingRow.Id}) and ({thingId}) don't match.");
            }
        }

        var needsRetrieval = FillRow(existingRow, newData) > 0;

        if (rootWord.PronunciationAudioLinks == null) {
            return new KifaActionResult<MemriseWord>(existingRow);
        }

        existingRow.FillAudios();

        var audios = rootWord.GetTopPronunciationAudioLinks()
            .Where(link => !WithDifferentArticle(link, word.Id)).Take(3).ToList();
        Logger.Debug($"Will upload {audios.Count} audios:");
        foreach (var audio in audios) {
            Logger.Debug(audio);
        }

        needsRetrieval = needsRetrieval || (audios.Count == 0
            ? ClearAllAudios(existingRow)
            : UploadAudios(existingRow, audios));

        if (needsRetrieval) {
            existingRow = GetExistingRow(word);
            if (existingRow == null) {
                Logger.Error($"Failed to get filled row of id {word}.");
                return new KifaActionResult<MemriseWord>(KifaActionStatus.Error,
                    $"failed to add word {word.Id}");
            }

            existingRow.FillAudios();
        }

        return new KifaActionResult<MemriseWord>(existingRow);
    }

    static readonly Regex WordArticlePattern = new Regex("^(der|die|das) .*");
    static readonly Regex LinkArticlePattern = new Regex("/(der|die|das)_.*");

    static bool WithDifferentArticle(string link, string goetheGermanWord) {
        var wordArticle = WordArticlePattern.Match(goetheGermanWord);
        if (!wordArticle.Success) {
            return false;
        }

        var linkArticle = LinkArticlePattern.Match(link);
        if (!linkArticle.Success) {
            return false;
        }

        return linkArticle.Groups[1].Value != wordArticle.Groups[1].Value;
    }

    bool UploadAudios(MemriseWord originalWord, List<string> audios) {
        var audioFiles = audios.Select(audio => new KifaFile(audio)).ToList();
        audioFiles.ForEach(file => file.Add(false));
        var keys = audioFiles.Select(file => (file.FileInfo!.Size!.Value, file.FileInfo.Md5!))
            .ToHashSet();
        var modified = RemoveUnneededAudios(originalWord, keys);

        foreach (var audioFile in audioFiles) {
            var info = audioFile.FileInfo!;

            if (originalWord.Audios?.Any(audio
                    => audio.Size == info.Size && audio.Md5 == info.Md5) ?? false) {
                Logger.Debug(
                    $"{audioFile} for {originalWord.Data["1"]} ({originalWord.Id}) already exists.");
                continue;
            }

            Logger.Debug(
                $"Uploading {audioFile} for {originalWord.Data["1"]} ({originalWord.Id}).");
            new UploadAudioRpc {
                HttpClient = HttpClient
            }.Invoke(Course.DatabaseUrl, originalWord.Id, Course.Columns["Audios"], CsrfToken,
                audioFile.ReadAsBytes());
            modified = true;
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        return modified;
    }

    bool ClearAllAudios(MemriseWord originalWord) {
        if (originalWord.Audios == null) {
            Logger.Debug(
                $"No audio files to remove for {originalWord.Data["1"]} ({originalWord.Id}).");
            return false;
        }

        Logger.Debug(
            $"Remove all {originalWord.Audios.Count} audio files for {originalWord.Data["1"]} ({originalWord.Id}).");
        foreach (var _ in originalWord.Audios) {
            var result = new RemoveAudioRpc {
                HttpClient = HttpClient
            }.Invoke(Course.BaseUrl, originalWord.Id, Course.Columns["Audios"], "1")?.Success;
            Logger.Debug($"Result of removing file 1: {result}");
        }

        return originalWord.Audios.Count > 0;
    }

    bool RemoveUnneededAudios(MemriseWord originalWord,
        HashSet<(long size, string md5)> audioFiles) {
        if (originalWord.Audios == null) {
            return false;
        }

        var foundOnes = new HashSet<(long size, string md5)>();
        var toRemove = new List<int>();
        for (var i = 0; i < originalWord.Audios.Count; i++) {
            var audio = originalWord.Audios[i];
            var key = (audio.Size, audio.Md5!);
            if (foundOnes.Contains(key)) {
                toRemove.Add(i + 1);
                continue;
            }

            if (!audioFiles.Contains(key)) {
                toRemove.Add(i + 1);
                continue;
            }

            foundOnes.Add(key);
        }

        Logger.Debug(
            $"Remove {toRemove.Count} audio files for {originalWord.Data["1"]} ({originalWord.Id}): {string.Join(", ", toRemove)}");
        foreach (var fileId in (toRemove as IEnumerable<int>).Reverse()) {
            var result = new RemoveAudioRpc {
                    HttpClient = HttpClient
                }.Invoke(Course.BaseUrl, originalWord.Id, Course.Columns["Audios"],
                    fileId.ToString())
                ?.Success;
            Logger.Debug($"Result of removing file {fileId}: {result}");
        }

        return toRemove.Count > 0;
    }

    MemriseWord? GetExistingRow(GoetheGermanWord word)
        => Course.GetPotentialExistingRows(TrimBracket(word.Id))
            .FirstOrDefault(mem => SameWord(mem, word)) ?? Course
            .GetPotentialExistingRows(TrimBracket(word.Meaning))
            .FirstOrDefault(mem => SameWord(mem, word));

    bool SameWord(MemriseWord? memriseWord, GoetheGermanWord goetheGermanWord)
        => memriseWord != null && memriseWord.Data[Course.Columns["German"]] == goetheGermanWord.Id;

    static string TrimBracket(string content) {
        var reg = new Regex(@"^(\(.*\) )?(.*)( \(.*\))?$");
        return reg.Match(content).Groups[2].Value;
    }

    string? FillBasicWord(Dictionary<string, string> newData)
        => new AddWordRpc {
            HttpClient = HttpClient
        }.Invoke(Course.DatabaseId, Course.BaseUrl, newData)?.Thing.Id.ToString();

    int FillRow(MemriseWord originalData, Dictionary<string, string> newData) {
        var updatedFields = 0;
        foreach (var (dataKey, newValue) in newData) {
            if (!SameText(originalData.Data.GetValueOrDefault(dataKey), newValue)) {
                new UpdateWordRpc {
                    HttpClient = HttpClient
                }.Invoke(Course.DatabaseUrl, originalData.Id, dataKey, newValue);
                updatedFields++;
            }
        }

        return updatedFields;
    }

    static readonly Regex Spaces = new("\\s+");

    static bool SameText(string? oldValue, string newValue) {
        if (oldValue == null) {
            return true;
        }

        var normalizedOldValue = Spaces.Replace(oldValue, " ");
        var normalizedNewValue = Spaces.Replace(newValue, " ");
        return normalizedNewValue == normalizedOldValue;
    }

    Dictionary<string, string> GetDataFromWord(GoetheGermanWord word, GermanWord? baseWord) {
        var data = new Dictionary<string, string> {
            { Course.Columns["German"], word.Id },
            { Course.Columns["English"], word.Meaning }
        };

        data[Course.Columns["Form"]] = word.Form ?? "";

        data[Course.Columns["Pronunciation"]] =
            baseWord?.Pronunciation != null ? $"[{baseWord.Pronunciation}]" : "";

        data[Course.Columns["Examples"]] =
            word.Examples?.Count > 0 && !word.Examples[0].StartsWith("example")
                ? string.Join(LineBreak,
                    word.Examples.Select((example, index) => $"{index + 1}. {example}"))
                : "";

        data[Course.Columns["Etymology"]] = baseWord?.Etymology != null
            ? string.Join(LineBreak,
                baseWord.Etymology.Select(segment
                    => segment + ": " + (WordClient.Get(segment)?.Meaning ?? "<unknown>")))
            : "";

        return data;
    }

    public void Dispose() {
        httpClient?.Dispose();
    }
}
