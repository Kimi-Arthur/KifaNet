using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FluentAssertions;
using Kifa.Service;
using Xunit;

namespace Kifa.Web.Api.Tests;

public class TestFillDataModel : DataModel, WithModelId<TestFillDataModel> {
    public static string ModelId => "test_fills";

    public static KifaServiceClient<TestFillDataModel> Client { get; set; } =
        new KifaServiceRestClient<TestFillDataModel>();

    public static TimeSpan? GlobalRefreshInterval { get; set; }
    public static DataVersion? GlobalForceRefreshBefore { get; set; }
    public static Dictionary<string, string> UpstreamLinks { get; } = new();

    public string? Content { get; set; }
    public string? RemoteSourceContent { get; set; }
    public string? UpstreamContent { get; set; }

    public override bool FillByDefault => true;

    public override TimeSpan? RefreshInterval => GlobalRefreshInterval;

    public override DataVersion? ForceRefreshBefore => GlobalForceRefreshBefore;

    public override void Fill() {
        Content = RemoteSourceContent;

        if (Id != null && UpstreamLinks.TryGetValue(Id, out var upstreamId)) {
            var upstream = Client.Get(upstreamId);
            if (upstream != null && NeedRefreshFrom(upstream)) {
                UpstreamContent = upstream.Content;
            }
        }
    }
}

public class NonUpstreamDataModel : DataModel, WithModelId<NonUpstreamDataModel> {
    public static string ModelId => "non_upstream_items";

    public static KifaServiceClient<NonUpstreamDataModel> Client { get; set; } =
        new KifaServiceRestClient<NonUpstreamDataModel>();

    public string? Content { get; set; }
}

public class VersioningAndFreshnessTests : IDisposable {
    readonly string folder;
    readonly KifaServiceJsonClient<TestFillDataModel> client = new();

    public VersioningAndFreshnessTests() {
        folder = $"{Path.GetTempPath()}/{nameof(VersioningAndFreshnessTests)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        client.DataFolder = folder;
        TestFillDataModel.Client = client;
        TestFillDataModel.GlobalRefreshInterval = null;
        TestFillDataModel.GlobalForceRefreshBefore = null;
        TestFillDataModel.UpstreamLinks.Clear();
    }

    [Fact]
    public void InitialFillSetsVersionAndLastRefreshed() {
        var id = nameof(InitialFillSetsVersionAndLastRefreshed);
        var initialTime = DateTimeOffset.UtcNow;

        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "initial content"
        };

        client.Set(model);

        var data = client.Get(id);
        data.Should().NotBeNull();
        data!.Content.Should().Be("initial content");
        data.Metadata.Should().NotBeNull();
        data.Metadata!.Version.Should().NotBeNull();
        data.Metadata.Version!.Value.Should().BeOnOrAfter(initialTime);
        data.Metadata.LastRefreshed.Should().Be(data.Metadata.Version);
    }

    [Fact]
    public void ForceRefreshWithoutContentChangePreservesVersion() {
        var id = nameof(ForceRefreshWithoutContentChangePreservesVersion);
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "constant content"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        var originalVersion = firstGet!.Metadata!.Version;
        var originalLastRefreshed = firstGet.Metadata.LastRefreshed;

        Thread.Sleep(50);

        // Force refresh via refresh=true
        var secondGet = client.Get(id, refresh: true);
        secondGet.Should().NotBeNull();
        secondGet!.Metadata.Should().NotBeNull();

        // Version should remain identical because remote content did not change
        secondGet.Metadata!.Version.Should().Be(originalVersion);

        // LastRefreshed should advance to current timestamp
        secondGet.Metadata.LastRefreshed!.Value.Should().BeAfter(originalLastRefreshed!.Value);
    }

    [Fact]
    public void RefreshWithContentChangeUpdatesVersionAndLastRefreshed() {
        var id = nameof(RefreshWithContentChangeUpdatesVersionAndLastRefreshed);
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "old content"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        var originalVersion = firstGet!.Metadata!.Version;

        Thread.Sleep(50);

        // Update remote source content in disk model simulation
        var diskModel = client.Get(id);
        diskModel!.RemoteSourceContent = "new changed content";
        client.Update(diskModel);

        var secondGet = client.Get(id, refresh: true);
        secondGet.Should().NotBeNull();
        secondGet!.Content.Should().Be("new changed content");
        secondGet.Metadata.Should().NotBeNull();

        // Version should advance because content actually changed
        secondGet.Metadata!.Version!.Value.Should().BeAfter(originalVersion!.Value);
        secondGet.Metadata.LastRefreshed.Should().Be(secondGet.Metadata.Version);
    }

    [Fact]
    public void ForceRefreshBeforeUpdatesVersionEvenIfContentUnchanged() {
        var id = nameof(ForceRefreshBeforeUpdatesVersionEvenIfContentUnchanged);
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "same content"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        var originalVersion = firstGet!.Metadata!.Version;

        Thread.Sleep(50);

        // Simulate code logic update by setting ForceRefreshBefore after originalVersion
        var futureForceRefresh = (DataVersion) DateTimeOffset.UtcNow;
        TestFillDataModel.GlobalForceRefreshBefore = futureForceRefresh;

        // Get() should detect NeedRefresh because Version < ForceRefreshBefore
        var secondGet = client.Get(id);
        secondGet.Should().NotBeNull();
        secondGet!.Metadata.Should().NotBeNull();

        // Version must update because ForceRefreshBefore invalidated the previous version
        secondGet.Metadata!.Version!.Value.Should().BeAfter(originalVersion!.Value);
        secondGet.Metadata.Version!.Value.Should().BeOnOrAfter(futureForceRefresh.Value);
    }

    [Fact]
    public void RefreshIntervalTriggersRefreshWhenElapsed() {
        var id = nameof(RefreshIntervalTriggersRefreshWhenElapsed);
        TestFillDataModel.GlobalRefreshInterval = TimeSpan.FromMilliseconds(50);
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "data"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        firstGet!.NeedRefresh().Should().BeFalse();

        // Wait for interval to elapse
        Thread.Sleep(100);

        firstGet.NeedRefresh().Should().BeTrue();

        var refreshedGet = client.Get(id);
        refreshedGet.Should().NotBeNull();
        refreshedGet!.NeedRefresh().Should().BeFalse();
    }

    [Fact]
    public void UpstreamFreshnessCheckInFillRefreshesDependentRegion() {
        var upstreamId = $"{nameof(UpstreamFreshnessCheckInFillRefreshesDependentRegion)}_upstream";
        var downstreamId = $"{nameof(UpstreamFreshnessCheckInFillRefreshesDependentRegion)}_downstream";

        TestFillDataModel.UpstreamLinks[downstreamId] = upstreamId;

        var upstream = new TestFillDataModel {
            Id = upstreamId,
            RemoteSourceContent = "upstream v1"
        };
        client.Set(upstream);

        Thread.Sleep(50);

        var downstream = new TestFillDataModel {
            Id = downstreamId,
            RemoteSourceContent = "downstream content"
        };
        client.Set(downstream);

        var downstreamData = client.Get(downstreamId);
        downstreamData!.UpstreamContent.Should().Be("upstream v1");

        Thread.Sleep(50);

        // Downstream refreshed when upstream has NOT changed: region is skipped, version unchanged
        var downstreamVersionBefore = downstreamData.Metadata!.Version;
        var refreshedDownstream = client.Get(downstreamId, refresh: true);
        refreshedDownstream!.UpstreamContent.Should().Be("upstream v1");
        refreshedDownstream.Metadata!.Version.Should().Be(downstreamVersionBefore);

        Thread.Sleep(50);

        // Upstream changes content and refreshes, bumping upstream Version
        var upstreamData = client.Get(upstreamId);
        upstreamData!.RemoteSourceContent = "upstream v2";
        client.Update(upstreamData);
        client.Get(upstreamId, refresh: true);

        // Downstream NeedRefresh is false on its own (schedule managed otherwise)
        refreshedDownstream.NeedRefresh().Should().BeFalse();

        // When downstream is refreshed, Fill() checks NeedRefreshFrom(upstream) and updates UpstreamContent
        var updatedDownstream = client.Get(downstreamId, refresh: true);
        updatedDownstream!.UpstreamContent.Should().Be("upstream v2");
        updatedDownstream.Metadata!.Version!.Value.Should().BeAfter(downstreamVersionBefore!.Value);
    }

    [Fact]
    public void LegacyJsonWithIntegerVersionAndFreshnessBlockIsMigratedCleanly() {
        Directory.CreateDirectory($"{folder}/test_fills");
        var id = nameof(LegacyJsonWithIntegerVersionAndFreshnessBlockIsMigratedCleanly);

        // Raw legacy JSON representation
        var legacyJson = @$"{{
  ""id"": ""{id}"",
  ""remote_source_content"": ""migrated content"",
  ""content"": ""migrated content"",
  ""metadata"": {{
    ""version"": 18,
    ""freshness"": {{
      ""next_refresh"": ""2020-01-01T00:00:00Z""
    }}
  }}
}}";
        var filePath = $"{folder}/test_fills/{id}.json";
        File.WriteAllText(filePath, legacyJson);

        // When reading the file, legacy integer version deserializes to null, triggering re-fill
        var data = client.Get(id);
        data.Should().NotBeNull();
        data!.Metadata.Should().NotBeNull();
        data.Metadata!.Version.Should().NotBeNull();
        data.Metadata.Version!.Value.Should().BeOnOrAfter(DateTimeOffset.UtcNow.AddMinutes(-1));

        // Read the written JSON on disk to verify legacy freshness is stripped and version is pure UTC digits
        var writtenJson = File.ReadAllText(filePath);
        writtenJson.Should().NotContain("freshness");
        writtenJson.Should().NotContain("\"version\": 18");
        writtenJson.Should().Contain("\"version\":");
    }

    [Fact]
    public void PureNumberVersionWithMicrosecondsSerializesAndDeserializesCorrectly() {
        var metadata = new DataMetadata {
            Version = new DateTimeOffset(2026, 8, 31, 15, 41, 26, 123, 456, TimeSpan.FromHours(2))
        };

        var json = metadata.ToJson();
        // Should serialize in UTC without timezone as pure 20-digit number string: 20260831134126123456
        json.Should().Contain("\"version\":\"20260831134126123456\"");

        var deserialized = json.FromJson<DataMetadata>();
        deserialized.Should().NotBeNull();
        deserialized!.Version.Should().Be(new DateTimeOffset(2026, 8, 31, 13, 41, 26, 123, 456, TimeSpan.Zero));
    }

    [Fact]
    public void PureNumberIntegerVersionDeserializesCorrectly() {
        var json = @"{ ""version"": 20260831154126 }";
        var deserialized = json.FromJson<DataMetadata>();
        deserialized.Should().NotBeNull();
        deserialized!.Version.Should().Be(new DateTimeOffset(2026, 8, 31, 15, 41, 26, TimeSpan.Zero));
    }

    [Fact]
    public void LegacyIsoStringVersionDeserializesCorrectly() {
        var json = @"{ ""version"": ""2026-08-29T12:00:00.000000+02:00"" }";
        var deserialized = json.FromJson<DataMetadata>();
        deserialized.Should().NotBeNull();
        deserialized!.Version.Should().Be(new DateTimeOffset(2026, 8, 29, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void EqualsIgnoresMetadataVersionDifferences() {
        var m1 = new TestFillDataModel {
            Id = "item1",
            Content = "abc",
            Metadata = new DataMetadata { Version = DateTimeOffset.UtcNow }
        };

        var m2 = new TestFillDataModel {
            Id = "item1",
            Content = "abc",
            Metadata = new DataMetadata { Version = DateTimeOffset.UtcNow.AddDays(1) }
        };

        // Identical content with different metadata
        m1.Equals(m2).Should().BeTrue();

        // Modified property
        m2.Content = "def";
        m1.Equals(m2).Should().BeFalse();

        // Null comparison
        m1.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void NonUpstreamDataModelsSkipVersioning() {
        var nonUpstreamClient = new KifaServiceJsonClient<NonUpstreamDataModel> {
            DataFolder = folder
        };
        NonUpstreamDataModel.Client = nonUpstreamClient;

        var id = nameof(NonUpstreamDataModelsSkipVersioning);
        var model = new NonUpstreamDataModel {
            Id = id,
            Content = "user provided data"
        };

        nonUpstreamClient.Set(model);

        var data = nonUpstreamClient.Get(id);
        data.Should().NotBeNull();
        data!.Content.Should().Be("user provided data");
        data.NeedRefresh().Should().BeFalse();
        data.Metadata.Should().BeNull();

        // Check on-disk file: should not have $metadata or version
        var filePath = $"{folder}/non_upstream_items/{id}.json";
        var writtenJson = File.ReadAllText(filePath);
        writtenJson.Should().NotContain("metadata");
        writtenJson.Should().NotContain("version");

        // Force refresh should throw NoNeedToFillException internally and not write metadata
        var refreshed = nonUpstreamClient.Get(id, refresh: true);
        refreshed.Should().NotBeNull();
        refreshed!.Metadata.Should().BeNull();
    }

    public void Dispose() {
        TestFillDataModel.GlobalRefreshInterval = null;
        TestFillDataModel.GlobalForceRefreshBefore = null;
        TestFillDataModel.UpstreamLinks.Clear();
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
