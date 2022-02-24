using System;
using System.Collections.Generic;
using Kifa.Service;

namespace Kifa.Games.BreathOfTheWild;

public class Game : DataModel<Game> {
    public const string ModelId = "games/botw";

    public string Name { get; set; }
    public string Notes { get; set; }
    public GameMode Mode { get; set; }

    public SortedDictionary<string, KorokSeedState> KorokSeeds { get; set; }
    public SortedDictionary<string, ShrineState> Shrines { get; set; }
}

public enum GameMode {
    Normal,
    Master
}

public class ShrineState {
    public DateTime? Found { get; set; }
    public DateTime? Solved { get; set; }
}

public class KorokSeedState {
    public DateTime? Found { get; set; }
}
