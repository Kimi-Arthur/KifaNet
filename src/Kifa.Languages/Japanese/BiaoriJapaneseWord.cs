using System.Text.Json.Serialization;
using Kifa.Service;
using YamlDotNet.Serialization;

namespace Kifa.Languages.Japanese;

public class BiaoriJapaneseWord : DataModel {
    public const string ModelId = "japanese/biaori/words";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestClient();

    public interface ServiceClient : KifaServiceClient<BiaoriJapaneseWord> {
    }

    public class RestClient : KifaServiceRestClient<BiaoriJapaneseWord>, ServiceClient {
    }

    #endregion

    public string Kana { get; set; } = "";
    public string Chinese { get; set; } = "";
    public WordType Type { get; set; } = WordType.Unknown;

    public int Book { get; set; }
    public int Unit { get; set; }
    public int Lesson { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string AudioFile => $"标日app/book{Book}-unit{Unit}/lesson{Lesson}/lesson_words.pepm";

    public double AudioStart { get; set; }
    public double AudioEnd { get; set; }
}
