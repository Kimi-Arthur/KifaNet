using System.IO;
using Xunit;

namespace Kifa.Media.MpegDash.Tests; 

public class MpegDashFileTests {
    [Fact]
    public void DeserializeSkyChTest() {
        using var stream = File.OpenRead("KELLMFAAMAOJEBDG.mpd");
        var mpeg = new MpegDashFile(stream);
        Assert.Equal(3, mpeg.DashInfo.Period.AdaptationSet.Count);
    }

    [Fact]
    public void LinkTest() {
        var audioInit =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=Init)";
        var videoInit =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=Init)";
        var video0 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=0)";
        var video1 = "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=40000000)";
        var video2 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=80000000)";
        var audio0 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=141)";
        var audio1 = "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=96397)";
        var audio2 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=192653)";
    }
}
