using Kifa.Service;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Kifa.Languages.Japanese;

public class BiaoriJapaneseWord : DataModel, WithModelId {
    public static string ModelId => "japanese/biaori/words";

    #region Clients

    public static ServiceClient Client { get; set; } = new RestClient();

    public interface ServiceClient : KifaServiceClient<BiaoriJapaneseWord> {
    }

    public class RestClient : KifaServiceRestClient<BiaoriJapaneseWord>, ServiceClient {
    }

    #endregion

    public string Kana { get; set; } = "";
    public string Chinese { get; set; } = "";
    public string Type { get; set; } = "/";

    public int Book { get; set; }
    public int Unit { get; set; }
    public int Lesson { get; set; }

    [JsonIgnore]
    [YamlIgnore]
    public string AudioFile => $"标日app/book{Book}-unit{Unit}/lesson{Lesson}/lesson_words.pepm";

    public double AudioStart { get; set; }
    public double AudioEnd { get; set; }
}
