using System;
using System.Collections.Generic;
using FluentAssertions;
using Kifa.IO;
using Kifa.Service;
using Kifa.Tools.DataUtil;
using Kifa.Tools.DataUtil.Commands;
using Xunit;

namespace Kifa.Tools.Tests;

public class CallCommandTests : IDisposable {
    class FakeRpcFileServiceClient : BaseKifaServiceClient<FileInformation>, KifaRpcClient {
        public string? LastAction { get; private set; }
        public object? LastParameters { get; private set; }
        public KifaActionResult ResultToReturn { get; set; } = KifaActionResult.Success();

        public override SortedDictionary<string, FileInformation> List(string folder = "",
            bool recursive = true, KifaDataOptions? options = null) => new();

        public override FileInformation? Get(string id, KifaDataOptions? options = null) => null;
        public override KifaActionResult Set(FileInformation data) => KifaActionResult.Success();
        public override KifaActionResult Update(FileInformation data) => KifaActionResult.Success();
        public override KifaActionResult Delete(string id) => KifaActionResult.Success();
        public override KifaActionResult Link(string targetId, string linkId) => KifaActionResult.Success();

        public KifaActionResult Call(string action, object? parameters = null) {
            LastAction = action;
            LastParameters = parameters;
            return ResultToReturn;
        }

        public TResponse? Call<TResponse>(string action, object? parameters = null) => default;
    }

    readonly FakeRpcFileServiceClient fakeClient = new();

    public CallCommandTests() {
        DataChef<FileInformation>.Client = fakeClient;
    }

    public void Dispose() {
        DataChef<FileInformation>.Client = new KifaServiceRestClient<FileInformation>();
    }

    [Fact]
    public void DataChefCallTest_SuccessWithoutParameters() {
        var chef = DataChef.GetChef("files").Checked();
        var result = chef.Call("fix");

        result.Status.Should().Be(KifaActionStatus.OK);
        fakeClient.LastAction.Should().Be("fix");
        fakeClient.LastParameters.Should().BeNull();
    }

    [Fact]
    public void DataChefCallTest_SuccessWithJsonParameters() {
        var chef = DataChef.GetChef("files").Checked();
        var result = chef.Call("fix", "{\"fields_to_merge\": [\"Locations\"]}");

        result.Status.Should().Be(KifaActionStatus.OK);
        fakeClient.LastAction.Should().Be("fix");
        fakeClient.LastParameters.Should().NotBeNull();
        var dict = fakeClient.LastParameters as Dictionary<object, object>;
        dict.Should().NotBeNull();
        dict!["fields_to_merge"].As<List<object>>().Should().Contain("Locations");
    }

    [Fact]
    public void CallCommandExecuteTest_SuccessWithoutParameters() {
        var command = new CallCommand {
            Target = "files.fix"
        };

        var exitCode = command.Execute();
        exitCode.Should().Be(0);
        fakeClient.LastAction.Should().Be("fix");
        fakeClient.LastParameters.Should().BeNull();
    }

    [Fact]
    public void CallCommandExecuteTest_SuccessWithDashPParameters() {
        var command = new CallCommand {
            Target = "files.fix",
            Parameters = "{\"fields_to_merge\": [\"Locations\"]}"
        };

        var exitCode = command.Execute();
        exitCode.Should().Be(0);
        fakeClient.LastAction.Should().Be("fix");
        fakeClient.LastParameters.Should().NotBeNull();
    }

    [Fact]
    public void CallCommandExecuteTest_InvalidTargetFormat() {
        var command = new CallCommand {
            Target = "invalid_target_without_dot"
        };

        var exitCode = command.Execute();
        exitCode.Should().Be(1);
    }

    [Fact]
    public void CallCommandExecuteTest_UnknownType() {
        var command = new CallCommand {
            Target = "nonexistent_type.fix"
        };

        var exitCode = command.Execute();
        exitCode.Should().Be(1);
    }
}
