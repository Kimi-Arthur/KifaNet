using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Memrise;

public class MemriseWord : DataModel {
    public const string ModelId = "memrise/words";

    static MemriseWordServiceClient? client;
    public static MemriseWordServiceClient Client => client ??= new MemriseWordRestServiceClient();

    public Dictionary<string, string> Data { get; set; }

    public List<MemriseAudio> Audios { get; set; }
}

public class MemriseAudio {
    #region public late string Link { get; set; }

    string? link;

    public string Link {
        get => Late.Get(link);
        set => Late.Set(ref link, value);
    }

    #endregion

    public long Size { get; set; }
    public string? Md5 { get; set; }
}

public interface MemriseWordServiceClient : KifaServiceClient<MemriseWord> {
}

public class MemriseWordRestServiceClient : KifaServiceRestClient<MemriseWord>,
    MemriseWordServiceClient {
}
