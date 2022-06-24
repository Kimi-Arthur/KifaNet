using System.Collections.Generic;

namespace Kifa.Memrise;

public class MemriseWord {
    public string ThingId { get; set; }

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
