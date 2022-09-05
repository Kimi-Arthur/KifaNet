using System.Collections.Generic;
using System.Xml.Serialization;

namespace Kifa.Media.MpegDash;

[XmlRoot(ElementName = "AssetIdentifier", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class AssetIdentifier {
    [XmlAttribute(AttributeName = "schemeIdUri")]
    public string SchemeIdUri { get; set; }

    [XmlAttribute(AttributeName = "value")]
    public string Value { get; set; }
}

[XmlRoot(ElementName = "S", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class S {
    [XmlAttribute(AttributeName = "d")]
    public string D { get; set; }

    [XmlAttribute(AttributeName = "r")]
    public string R { get; set; }

    [XmlAttribute(AttributeName = "t")]
    public string T { get; set; }
}

[XmlRoot(ElementName = "SegmentTimeline", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class SegmentTimeline {
    [XmlElement(ElementName = "S", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public List<S> S { get; set; }
}

[XmlRoot(ElementName = "SegmentTemplate", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class SegmentTemplate {
    [XmlElement(ElementName = "SegmentTimeline", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public SegmentTimeline SegmentTimeline { get; set; }

    [XmlAttribute(AttributeName = "timescale")]
    public string Timescale { get; set; }

    [XmlAttribute(AttributeName = "media")]
    public string Media { get; set; }

    [XmlAttribute(AttributeName = "initialization")]
    public string Initialization { get; set; }

    [XmlAttribute(AttributeName = "presentationTimeOffset")]
    public string PresentationTimeOffset { get; set; }
}

[XmlRoot(ElementName = "Representation", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class Representation {
    [XmlAttribute(AttributeName = "id")]
    public string Id { get; set; }

    [XmlAttribute(AttributeName = "bandwidth")]
    public string Bandwidth { get; set; }

    [XmlAttribute(AttributeName = "codecs")]
    public string Codecs { get; set; }

    [XmlAttribute(AttributeName = "width")]
    public string Width { get; set; }

    [XmlAttribute(AttributeName = "height")]
    public string Height { get; set; }

    [XmlAttribute(AttributeName = "frameRate")]
    public string FrameRate { get; set; }

    [XmlAttribute(AttributeName = "sar")]
    public string Sar { get; set; }

    [XmlAttribute(AttributeName = "audioSamplingRate")]
    public string AudioSamplingRate { get; set; }
}

[XmlRoot(ElementName = "AdaptationSet", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class AdaptationSet {
    [XmlElement(ElementName = "SegmentTemplate", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public SegmentTemplate SegmentTemplate { get; set; }

    [XmlElement(ElementName = "Representation", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public List<Representation> Representation { get; set; }

    [XmlAttribute(AttributeName = "id")]
    public string Id { get; set; }

    [XmlAttribute(AttributeName = "group")]
    public string Group { get; set; }

    [XmlAttribute(AttributeName = "bitstreamSwitching")]
    public string BitstreamSwitching { get; set; }

    [XmlAttribute(AttributeName = "segmentAlignment")]
    public string SegmentAlignment { get; set; }

    [XmlAttribute(AttributeName = "contentType")]
    public string ContentType { get; set; }

    [XmlAttribute(AttributeName = "mimeType")]
    public string MimeType { get; set; }

    [XmlAttribute(AttributeName = "maxWidth")]
    public string MaxWidth { get; set; }

    [XmlAttribute(AttributeName = "maxHeight")]
    public string MaxHeight { get; set; }

    [XmlAttribute(AttributeName = "par")]
    public string Par { get; set; }

    [XmlAttribute(AttributeName = "maxFrameRate")]
    public string MaxFrameRate { get; set; }

    [XmlAttribute(AttributeName = "startWithSAP")]
    public string StartWithSAP { get; set; }

    [XmlElement(ElementName = "AudioChannelConfiguration",
        Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public AudioChannelConfiguration AudioChannelConfiguration { get; set; }

    [XmlAttribute(AttributeName = "lang")]
    public string Lang { get; set; }
}

[XmlRoot(ElementName = "AudioChannelConfiguration", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class AudioChannelConfiguration {
    [XmlAttribute(AttributeName = "schemeIdUri")]
    public string SchemeIdUri { get; set; }

    [XmlAttribute(AttributeName = "value")]
    public string Value { get; set; }
}

[XmlRoot(ElementName = "Period", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class Period {
    [XmlElement(ElementName = "AssetIdentifier", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public AssetIdentifier AssetIdentifier { get; set; }

    [XmlElement(ElementName = "AdaptationSet", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public List<AdaptationSet> AdaptationSet { get; set; }

    [XmlAttribute(AttributeName = "id")]
    public string Id { get; set; }

    [XmlAttribute(AttributeName = "start")]
    public string Start { get; set; }
}

[XmlRoot(ElementName = "MPD", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
public class DashInfo {
    [XmlElement(ElementName = "Period", Namespace = "urn:mpeg:dash:schema:mpd:2011")]
    public Period Period { get; set; }

    [XmlAttribute(AttributeName = "xmlns")]
    public string Xmlns { get; set; }

    [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
    public string Xsi { get; set; }

    [XmlAttribute(AttributeName = "profiles")]
    public string Profiles { get; set; }

    [XmlAttribute(AttributeName = "type")]
    public string Type { get; set; }

    [XmlAttribute(AttributeName = "availabilityStartTime")]
    public string AvailabilityStartTime { get; set; }

    [XmlAttribute(AttributeName = "mediaPresentationDuration")]
    public string MediaPresentationDuration { get; set; }

    [XmlAttribute(AttributeName = "maxSegmentDuration")]
    public string MaxSegmentDuration { get; set; }

    [XmlAttribute(AttributeName = "minBufferTime")]
    public string MinBufferTime { get; set; }
}
