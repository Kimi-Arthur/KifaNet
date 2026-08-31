using System;
using FluentAssertions;
using Xunit;

namespace Kifa.Service.Tests;

public class DataVersionTests {
    [Fact]
    public void DataVersionSerializesToPureUtcNumbersWithMicroseconds() {
        var dto = new DateTimeOffset(2026, 8, 31, 15, 41, 26, 123, 456, TimeSpan.FromHours(2));
        var version = new DataVersion(dto);

        version.ToJson().Should().Be("20260831134126123456");
        version.ToString().Should().Be("20260831134126123456");
    }

    [Fact]
    public void DataVersionParsesPureNumbersCorrectly() {
        var version = DataVersion.Parse("20260831134126123456");
        version.Should().NotBeNull();
        version!.Value.Should().Be(new DateTimeOffset(2026, 8, 31, 13, 41, 26, 123, 456, TimeSpan.Zero));

        var versionSeconds = DataVersion.Parse("20260831134126");
        versionSeconds.Should().NotBeNull();
        versionSeconds!.Value.Should().Be(new DateTimeOffset(2026, 8, 31, 13, 41, 26, TimeSpan.Zero));

        var versionDate = DataVersion.Parse("20260831");
        versionDate.Should().NotBeNull();
        versionDate!.Value.Should().Be(new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DataVersionParsesLegacyIsoStringCorrectly() {
        var version = DataVersion.Parse("2026-08-29T12:00:00.000000+02:00");
        version.Should().NotBeNull();
        version!.Value.Should().Be(new DateTimeOffset(2026, 8, 29, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DataVersionIgnoresLegacySmallIntegers() {
        DataVersion.Parse("18").Should().BeNull();
        DataVersion.Parse("0").Should().BeNull();
        DataVersion.Parse("19991231").Should().BeNull();
        DataVersion.Parse(null).Should().BeNull();
        DataVersion.Parse("").Should().BeNull();
    }

    [Fact]
    public void ImplicitConversionsWorkSeamlessly() {
        DataVersion? v1 = "20260831134126123456";
        v1.Should().NotBeNull();
        v1!.Value.Hour.Should().Be(13);

        DateTimeOffset dto = v1!;
        dto.Should().Be(new DateTimeOffset(2026, 8, 31, 13, 41, 26, 123, 456, TimeSpan.Zero));

        DataVersion v2 = dto;
        v2.Value.Should().Be(dto);

        DataVersion v3 = new DateTime(2026, 8, 31, 13, 41, 26, DateTimeKind.Utc);
        v3.Value.Should().Be(new DateTimeOffset(2026, 8, 31, 13, 41, 26, TimeSpan.Zero));
    }

    [Fact]
    public void ComparisonAndEqualityOperatorsWorkCorrectly() {
        var v1 = new DataVersion(2026, 8, 31, 10, 0, 0);
        var v2 = new DataVersion(2026, 8, 31, 12, 0, 0);
        var v3 = new DataVersion(2026, 8, 31, 10, 0, 0);

        (v1 < v2).Should().BeTrue();
        (v1 <= v2).Should().BeTrue();
        (v2 > v1).Should().BeTrue();
        (v2 >= v1).Should().BeTrue();
        (v1 == v3).Should().BeTrue();
        (v1 != v2).Should().BeTrue();

        v1.Equals(v3).Should().BeTrue();
        v1.Equals(v2).Should().BeFalse();
        v1.CompareTo(v2).Should().BeNegative();
    }

    [Fact]
    public void TimeSpanAdditionWorksCorrectly() {
        var v1 = new DataVersion(2026, 8, 31, 10, 0, 0);
        var v2 = v1 + TimeSpan.FromHours(2);

        v2.Value.Should().Be(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void JsonSerializationAndDeserializationRoundTrips() {
        var metadata = new DataMetadata {
            Version = new DataVersion(2026, 8, 31, 13, 41, 26, 123, 456),
            LastRefreshed = new DataVersion(2026, 8, 31, 14, 0, 0)
        };

        var json = metadata.ToJson();
        json.Should().Contain("\"version\":\"20260831134126123456\"");
        json.Should().Contain("\"last_refreshed\":\"20260831140000000000\"");

        var deserialized = json.FromJson<DataMetadata>();
        deserialized.Should().NotBeNull();
        deserialized!.Version.Should().Be(metadata.Version);
        deserialized.LastRefreshed.Should().Be(metadata.LastRefreshed);
    }
}
