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
    public static bool GlobalShouldFailFill { get; set; }
    public static bool GlobalShouldThrowFailedToFill { get; set; }
    public static int GlobalFillCount { get; set; }
    public static Dictionary<string, string> UpstreamLinks { get; } = new();

    public string? Content { get; set; }
    public string? RemoteSourceContent { get; set; }
    public string? UpstreamContent { get; set; }
    public bool ShouldFailFill { get; set; }
    public bool ShouldThrowFailedToFill { get; set; }

    public override TimeSpan? RefreshInterval => GlobalRefreshInterval;

    public override DataVersion? ForceRefreshBefore => GlobalForceRefreshBefore;

    public override void Fill() {
        GlobalFillCount++;
        if (GlobalShouldThrowFailedToFill || ShouldThrowFailedToFill) {
            throw new FailedToFillException($"Simulated process failure for {Id}.");
        }

        if (GlobalShouldFailFill || ShouldFailFill) {
            throw new DataNotFoundException($"Simulated not found for {Id}.");
        }

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
        TestFillDataModel.GlobalRefreshInterval = TimeSpan.FromDays(7);
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
    public void ForceRefreshBeforePreservesVersionIfContentUnchangedAndAdvancesLastRefreshed() {
        var id = nameof(ForceRefreshBeforePreservesVersionIfContentUnchangedAndAdvancesLastRefreshed);
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "same content"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        var originalVersion = firstGet!.Metadata!.Version;
        var originalLastRefreshed = firstGet.Metadata.LastRefreshed;

        Thread.Sleep(50);

        // Simulate code logic update by setting ForceRefreshBefore after originalVersion
        var futureForceRefresh = (DataVersion) DateTimeOffset.UtcNow;
        TestFillDataModel.GlobalForceRefreshBefore = futureForceRefresh;

        // Get() should detect NeedRefresh because LastRefreshed < ForceRefreshBefore
        var secondGet = client.Get(id);
        secondGet.Should().NotBeNull();
        secondGet!.Metadata.Should().NotBeNull();

        // Version must NOT update because content remained unchanged
        secondGet.Metadata!.Version.Should().Be(originalVersion);

        // LastRefreshed must update past ForceRefreshBefore
        secondGet.Metadata.LastRefreshed!.Value.Should().BeAfter(originalLastRefreshed!.Value);
        secondGet.Metadata.LastRefreshed!.Value.Should().BeOnOrAfter(futureForceRefresh.Value);

        // Subsequent Get() does not re-fill because lastChecked >= ForceRefreshBefore
        secondGet.NeedRefresh().Should().BeFalse();
    }

    [Fact]
    public void ForceRefreshBeforeUpdatesVersionWhenContentChanges() {
        var id = nameof(ForceRefreshBeforeUpdatesVersionWhenContentChanges);
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "initial content"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        var originalVersion = firstGet!.Metadata!.Version;

        Thread.Sleep(50);

        // Simulate code logic change producing new content
        firstGet.RemoteSourceContent = "improved parsed content";
        client.Update(firstGet);

        var futureForceRefresh = (DataVersion) DateTimeOffset.UtcNow;
        TestFillDataModel.GlobalForceRefreshBefore = futureForceRefresh;

        var secondGet = client.Get(id);
        secondGet.Should().NotBeNull();
        secondGet!.Content.Should().Be("improved parsed content");
        secondGet.Metadata!.Version!.Value.Should().BeAfter(originalVersion!.Value);
        secondGet.Metadata.Version!.Value.Should().BeOnOrAfter(futureForceRefresh.Value);
        secondGet.Metadata.LastRefreshed.Should().Be(secondGet.Metadata.Version);
    }

    [Fact]
    public void UpstreamForceRefreshBeforeWithoutContentChangeDoesNotCascadeToDownstream() {
        var upstreamId =
            $"{nameof(UpstreamForceRefreshBeforeWithoutContentChangeDoesNotCascadeToDownstream)}_upstream";
        var downstreamId =
            $"{nameof(UpstreamForceRefreshBeforeWithoutContentChangeDoesNotCascadeToDownstream)}_downstream";

        TestFillDataModel.UpstreamLinks[downstreamId] = upstreamId;
        TestFillDataModel.GlobalRefreshInterval = TimeSpan.FromDays(7);

        var upstream = new TestFillDataModel {
            Id = upstreamId,
            RemoteSourceContent = "upstream v1"
        };
        client.Set(upstream);

        var originalUpstream = client.Get(upstreamId);
        var originalUpstreamVersion = originalUpstream!.Metadata!.Version;

        Thread.Sleep(50);

        var downstream = new TestFillDataModel {
            Id = downstreamId,
            RemoteSourceContent = "downstream content"
        };
        client.Set(downstream);

        var downstreamData = client.Get(downstreamId);
        downstreamData!.UpstreamContent.Should().Be("upstream v1");
        var downstreamVersionBefore = downstreamData.Metadata!.Version;

        Thread.Sleep(50);

        // Upstream gets a ForceRefreshBefore bump, but its remote content remains identical
        var futureForceRefresh = (DataVersion) DateTimeOffset.UtcNow;
        TestFillDataModel.GlobalForceRefreshBefore = futureForceRefresh;

        // Fetch upstream, triggering upstream re-fill
        var refreshedUpstream = client.Get(upstreamId);
        refreshedUpstream!.Metadata!.LastRefreshed!.Value.Should().BeOnOrAfter(futureForceRefresh.Value);
        // Upstream Version did NOT change
        refreshedUpstream.Metadata.Version.Should().Be(originalUpstreamVersion);

        // Refresh downstream: upstream version has not changed, so downstream skips updating upstream region
        var refreshedDownstream = client.Get(downstreamId, refresh: true);
        refreshedDownstream!.UpstreamContent.Should().Be("upstream v1");
        refreshedDownstream.Metadata!.Version.Should().Be(downstreamVersionBefore);
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
        TestFillDataModel.GlobalRefreshInterval = TimeSpan.FromDays(7);

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
    public void ModelWithoutRefreshIntervalAlwaysRefreshesByDefault() {
        var id = nameof(ModelWithoutRefreshIntervalAlwaysRefreshesByDefault);
        TestFillDataModel.GlobalRefreshInterval = null;
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "v1"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        firstGet.Should().NotBeNull();
        firstGet!.NeedRefresh().Should().BeTrue();

        // Simulate remote source updating content
        firstGet.RemoteSourceContent = "v2";
        client.Update(firstGet);

        // Next Get() without refresh: true should automatically refresh
        var secondGet = client.Get(id);
        secondGet.Should().NotBeNull();
        secondGet!.Content.Should().Be("v2");
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

    [Fact]
    public void DataNotFoundOnNewItemPersistsNotFoundTombstoneAndReturnsNull() {
        var id = nameof(DataNotFoundOnNewItemPersistsNotFoundTombstoneAndReturnsNull);
        TestFillDataModel.GlobalShouldFailFill = true;

        var item = client.Get(id);
        item.Should().BeNull();

        var filePath = $"{folder}/test_fills/{id}.json";
        File.Exists(filePath).Should().BeTrue();
        var writtenJson = File.ReadAllText(filePath);
        writtenJson.Should().Contain("\"status\": \"not_found\"");
        writtenJson.Should().Contain("version");

        // List should not include NotFound items
        var list = client.List();
        list.ContainsKey(id).Should().BeFalse();

        // Subsequent Get returns null without re-running Fill (FillCount does not increase)
        var countBefore = TestFillDataModel.GlobalFillCount;
        var secondGet = client.Get(id);
        secondGet.Should().BeNull();
        TestFillDataModel.GlobalFillCount.Should().Be(countBefore);

        // If upstream becomes available, a forced refresh recovers the item
        TestFillDataModel.GlobalShouldFailFill = false;
        var recovered = client.Get(id, refresh: true);
        recovered.Should().NotBeNull();
        recovered!.Metadata!.Status.Should().Be(DataStatus.OK);

        var updatedJson = File.ReadAllText(filePath);
        updatedJson.Should().NotContain("\"status\"");

        var listAfter = client.List();
        listAfter.ContainsKey(id).Should().BeTrue();
    }

    [Fact]
    public void DataNotFoundOnExistingItemSetsRemovedStatusAndPreservesData() {
        var id = nameof(DataNotFoundOnExistingItemSetsRemovedStatusAndPreservesData);
        var model = new TestFillDataModel {
            Id = id,
            RemoteSourceContent = "initial content"
        };

        client.Set(model);

        var firstGet = client.Get(id);
        firstGet.Should().NotBeNull();
        firstGet!.Metadata!.Status.Should().Be(DataStatus.OK);
        var originalVersion = firstGet.Metadata.Version;
        var originalLastRefreshed = firstGet.Metadata.LastRefreshed;

        var initialJson = File.ReadAllText($"{folder}/test_fills/{id}.json");
        initialJson.Should().NotContain("\"status\"");

        Thread.Sleep(50);

        // Simulate remote resource becoming unavailable (e.g. video deleted/removed)
        firstGet.ShouldFailFill = true;
        client.Update(firstGet);

        // Trigger refresh
        var secondGet = client.Get(id, refresh: true);
        secondGet.Should().NotBeNull();
        secondGet!.Content.Should().Be("initial content");
        secondGet.Metadata.Should().NotBeNull();
        secondGet.Metadata!.Status.Should().Be(DataStatus.Removed);

        // Version is preserved
        secondGet.Metadata.Version.Should().Be(originalVersion);

        // LastRefreshed is updated to current time
        secondGet.Metadata.LastRefreshed!.Value.Should().BeAfter(originalLastRefreshed!.Value);

        // File on disk has status: removed and last_refreshed
        var filePath = $"{folder}/test_fills/{id}.json";
        var writtenJson = File.ReadAllText(filePath);
        writtenJson.Should().Contain("\"status\": \"removed\"");
        writtenJson.Should().Contain("last_refreshed");

        // NeedRefresh is now false because LastRefreshed + RefreshInterval (7 days) > UtcNow
        secondGet.NeedRefresh().Should().BeFalse();

        // List still includes Removed items
        var list = client.List();
        list.ContainsKey(id).Should().BeTrue();

        // Next standard Get() returns the item without re-filling
        var countBefore = TestFillDataModel.GlobalFillCount;
        var thirdGet = client.Get(id);
        thirdGet.Should().NotBeNull();
        thirdGet!.Metadata!.Status.Should().Be(DataStatus.Removed);
        TestFillDataModel.GlobalFillCount.Should().Be(countBefore);
    }

    [Fact]
    public void DataNotFoundOnLegacyFileWithoutMetadataSetsRemovedStatusAndGivesVersion() {
        Directory.CreateDirectory($"{folder}/test_fills");
        var id = nameof(DataNotFoundOnLegacyFileWithoutMetadataSetsRemovedStatusAndGivesVersion);
        TestFillDataModel.GlobalShouldFailFill = true;

        var legacyJson = @$"{{
  ""id"": ""{id}"",
  ""content"": ""legacy preserved content""
}}";
        var filePath = $"{folder}/test_fills/{id}.json";
        File.WriteAllText(filePath, legacyJson);

        var data = client.Get(id);
        data.Should().NotBeNull();
        data!.Content.Should().Be("legacy preserved content");
        data.Metadata.Should().NotBeNull();
        data.Metadata!.Status.Should().Be(DataStatus.Removed);
        data.Metadata.Version.Should().NotBeNull();
        data.Metadata.Version!.Value.Should().BeOnOrAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
        data.Metadata.LastRefreshed.Should().Be(data.Metadata.Version);

        var list = client.List();
        list.ContainsKey(id).Should().BeTrue();

        var writtenJson = File.ReadAllText(filePath);
        writtenJson.Should().Contain("\"status\": \"removed\"");
        writtenJson.Should().Contain("\"version\":");
    }

    [Fact]
    public void DataNotFoundOnSettingLegacyModelWithoutMetadataSetsRemovedStatusAndGivesVersion() {
        var id = nameof(DataNotFoundOnSettingLegacyModelWithoutMetadataSetsRemovedStatusAndGivesVersion);
        var model = new TestFillDataModel {
            Id = id,
            Content = "set content",
            ShouldFailFill = true
        };

        client.Set(model);

        var data = client.Get(id);
        data.Should().NotBeNull();
        data!.Content.Should().Be("set content");
        data.Metadata.Should().NotBeNull();
        data.Metadata!.Status.Should().Be(DataStatus.Removed);
        data.Metadata.Version.Should().NotBeNull();
        data.Metadata.Version!.Value.Should().BeOnOrAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
        data.Metadata.LastRefreshed.Should().Be(data.Metadata.Version);
    }

    [Fact]
    public void FailedToFillPreservesStatusAndDoesNotWriteTombstone() {
        Directory.CreateDirectory($"{folder}/test_fills");
        var id = nameof(FailedToFillPreservesStatusAndDoesNotWriteTombstone);
        var filePath = $"{folder}/test_fills/{id}.json";
        File.WriteAllText(filePath, "{\n  \"content\": \"original legacy content\"\n}");

        var model = new TestFillDataModel {
            Id = id,
            Content = "original legacy content",
            ShouldThrowFailedToFill = true
        };

        client.Set(model);

        var data = client.Get(id);
        data.Should().NotBeNull();
        data!.Content.Should().Be("original legacy content");
        // Status should remain default OK (not Removed, not NotFound)
        data.Metadata?.Status.Should().Be(DataStatus.OK);
    }

    public void Dispose() {
        TestFillDataModel.GlobalRefreshInterval = null;
        TestFillDataModel.GlobalForceRefreshBefore = null;
        TestFillDataModel.GlobalShouldFailFill = false;
        TestFillDataModel.GlobalShouldThrowFailedToFill = false;
        TestFillDataModel.GlobalFillCount = 0;
        TestFillDataModel.UpstreamLinks.Clear();
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, true);
        }
    }
}
