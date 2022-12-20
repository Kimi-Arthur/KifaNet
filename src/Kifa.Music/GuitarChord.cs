using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kifa.Service;
using Svg;

namespace Kifa.Music;

public class GuitarChord : DataModel, WithModelId {
    public static string ModelId => "guitar/chords";

    static KifaServiceClient<GuitarChord> client;

    public static KifaServiceClient<GuitarChord> Client
        => client ??= new KifaServiceRestClient<GuitarChord>();

    /// Name of the chord. Can be like, `C`, `Cmaj7`, `Em` etc.
    public string Name { get; set; }

    /// Arrangements by each finger.
    public List<FingerArrangement> Arrangements { get; set; }

    public SvgDocument GetPicture() {
        using var svgStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{typeof(GuitarChord).Namespace}.chord.svg");
        var document = SvgDocument.Open<SvgDocument>(svgStream);

        var leftStrings = new HashSet<int> {
            1,
            2,
            3,
            4,
            5,
            6
        };

        var maxFret = Arrangements.Max(a => a.Fret);
        var minFret = 1;

        if (maxFret <= 4) {
            document.Children.Add(GetTopBar());
        } else {
            minFret = Arrangements.Where(a => a.Finger != 0).Min(a => a.Fret);
            document.Children.Add(GetFret(minFret));
        }

        foreach (var arrangement in Arrangements) {
            if (arrangement.Finger == 0) {
                foreach (var s in arrangement.Strings) {
                    document.Children.Add(GetOpenString(s));
                    leftStrings.Remove(s);
                }

                continue;
            }

            if (arrangement.Strings.Count > 1) {
                for (var s = arrangement.Strings.Min(); s < arrangement.Strings.Max(); s++) {
                    document.Children.Add(GetFingerBar(arrangement.Fret - minFret + 1, s));
                }
            }

            foreach (var s in arrangement.Strings) {
                document.Children.Add(GetFingering(arrangement.Finger,
                    arrangement.Fret - minFret + 1, s));
                leftStrings.Remove(s);
            }
        }

        foreach (var s in leftStrings) {
            document.Children.Add(GetCross(s));
        }

        return document;
    }

    static SvgElement GetFret(int minFret)
        => new SvgText {
            FontFamily = "sans-serif",
            FontSize = 24,
            TextAnchor = SvgTextAnchor.End,
            X = new SvgUnitCollection {
                32
            },
            Y = new SvgUnitCollection {
                78
            },
            Text = $"{minFret}"
        };


    static SvgElement GetTopBar()
        => new SvgUse {
            ReferencedElement = new Uri("#top_bar", UriKind.Relative)
        };

    static SvgElement GetOpenString(int s)
        => new SvgUse {
            ReferencedElement = new Uri("#open", UriKind.Relative),
            X = 48 + 32 * (6 - s)
        };

    static SvgElement GetCross(int s)
        => new SvgUse {
            ReferencedElement = new Uri("#cross", UriKind.Relative),
            X = 36 + 32 * (6 - s)
        };

    static SvgElement GetFingerBar(int fret, int s)
        => new SvgUse {
            ReferencedElement = new Uri("#fbar", UriKind.Relative),
            X = 16 + 32 * (6 - s),
            Y = 10 + 48 * fret
        };

    static SvgElement GetFingering(int finger, int fret, int s)
        => new SvgUse {
            ReferencedElement = new Uri($"#f{finger}", UriKind.Relative),
            X = 48 + 32 * (6 - s),
            Y = 24 + 48 * fret
        };
}

/// Finger arrangement of one finger on one string.
public class FingerArrangement {
    /// 指, finger to use on the string, open -> 0, thumb -> 5, index -> 1, etc.
    public int Finger { get; set; }

    /// 弦, which strings this finger is on, 1 - 6, from higher to lower pitch string.
    /// Can contain multiple elements for `barre chord`, ordered.
    public List<int> Strings { get; set; }

    /// 品, Which fret this finger should be placed onto. For open, it should be 0.
    public int Fret { get; set; }
}

public interface GuitarChordServiceClient : KifaServiceClient<GuitarChord> {
    SvgDocument GetPicture(string id);
}

public class GuitarChordRestServiceClient : KifaServiceRestClient<GuitarChord>,
    GuitarChordServiceClient {
    public SvgDocument GetPicture(string id)
        => SvgDocument.FromSvg<SvgDocument>(Call<string>("get_picture",
            new Dictionary<string, object> {
                { "id", id }
            }));
}
