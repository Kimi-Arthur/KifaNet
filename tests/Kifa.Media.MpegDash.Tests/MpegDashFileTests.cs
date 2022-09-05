using System.IO;
using Xunit;

namespace Kifa.Media.MpegDash.Tests;

public class MpegDashFileTests {
    [Fact]
    public void DeserializeSkyChTest() {
        using var stream = File.OpenRead("KELLMFAAMAOJEBDG.mpd");
        var mpeg = new MpegDashFile("https://tvoosa-pez0406.sctv.ch/dash/BKLHAGAAJGDOFBDG.mpd",
            stream);
        Assert.Equal(3, mpeg.DashInfo.Period.AdaptationSet.Count);
    }

    [Fact]
    public void LinkTest() {
        using var stream = File.OpenRead("3995788.mpd");
        var file = new MpegDashFile("https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd",
            stream);
        var links = file.GetLinks();

        var videoInit =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=Init)";
        var video0 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=0)";
        var video1 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=40000000)";
        var video2 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(7200000)/Fragments(video=80000000)";

        var videoLinks = links.VideoLinks;
        Assert.Equal(450, videoLinks.Count);
        Assert.Equal(videoInit, videoLinks[0]);
        Assert.Equal(video0, videoLinks[1]);
        Assert.Equal(video1, videoLinks[2]);
        Assert.Equal(video2, videoLinks[3]);

        var audioInit =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=Init)";
        var audio0 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=141)";
        var audio1 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=96397)";
        var audio2 =
            "https://tvoosa-pez0406.sctv.ch/dash/BMGNOFAADLLOEBDG.mpd/QualityLevels(96000)/Fragments(audio_482_qae=192653)";

        var audioLinks0 = links.AudioLinks[0];
        Assert.Equal(338, audioLinks0.Count);
        Assert.Equal(audioInit, audioLinks0[0]);
        Assert.Equal(audio0, audioLinks0[1]);
        Assert.Equal(audio1, audioLinks0[2]);
        Assert.Equal(audio2, audioLinks0[3]);
    }
}
