using System.Collections.Generic;
using FluentAssertions;
using Kifa.Service;
using Xunit;

namespace Kifa.Tools.Tests;

public class UpgradeCommandTests {
    class TestUpgradeCommand : UpgradeCommand {
        public List<string> ExecutedCommands { get; } = new();
        public int ReturnExitCode { get; set; } = 0;
        public string ReturnStdOut { get; set; } = "Tool 'Kifa.Tools.FileUtil' was successfully updated.";
        public string ReturnStdErr { get; set; } = "";
        public string? PackageIdToReturn { get; set; } = "Kifa.Tools.FileUtil";
        public string? CurrentVersionToReturn { get; set; } = "8.1.5";

        public override string? PackageId => PackageIdToReturn;
        public override string? CurrentVersion => CurrentVersionToReturn;

        public override ExecutionResult RunDotnetCommand(string arguments) {
            ExecutedCommands.Add(arguments);
            return new ExecutionResult {
                ExitCode = ReturnExitCode,
                StandardOutput = ReturnStdOut,
                StandardError = ReturnStdErr
            };
        }
    }

    [Fact]
    public void Execute_SelfUpgrade_UpgradesCurrentPackage() {
        var command = new TestUpgradeCommand {
            PackageIdToReturn = "Kifa.Tools.FileUtil"
        };

        var exitCode = command.Execute();

        exitCode.Should().Be(0);
        command.ExecutedCommands.Should().ContainSingle()
            .Which.Should().Be("tool update -g Kifa.Tools.FileUtil");
        command.Results.Should().ContainSingle(r => r.result.Status == KifaActionStatus.OK);
    }

    [Fact]
    public void Execute_AlreadyInstalled_ReturnsSkippedWithVersion() {
        var command = new TestUpgradeCommand {
            PackageIdToReturn = "Kifa.Tools.FileUtil",
            ReturnStdOut = "Tool 'Kifa.Tools.FileUtil' (version '8.1.5') is already installed."
        };

        var exitCode = command.Execute();

        exitCode.Should().Be(0);
        command.Results.Should().ContainSingle(r =>
            r.result.Status == KifaActionStatus.Skipped &&
            r.result.Message == "Tool 'Kifa.Tools.FileUtil' (version '8.1.5') is already installed.");
    }

    [Fact]
    public void Execute_AlreadyInstalledWithoutVersionInOutput_IncludesCurrentVersion() {
        var command = new TestUpgradeCommand {
            PackageIdToReturn = "Kifa.Tools.FileUtil",
            CurrentVersionToReturn = "8.1.5",
            ReturnStdOut = "Tool 'Kifa.Tools.FileUtil' is already installed."
        };

        var exitCode = command.Execute();

        exitCode.Should().Be(0);
        command.Results.Should().ContainSingle(r =>
            r.result.Status == KifaActionStatus.Skipped &&
            r.result.Message == "Tool 'Kifa.Tools.FileUtil' is already installed. (Current version: 8.1.5)");
    }

    [Fact]
    public void Execute_EmptyOutputWhenNotUpdated_ReturnsSkippedWithCurrentVersion() {
        var command = new TestUpgradeCommand {
            PackageIdToReturn = "Kifa.Tools.FileUtil",
            CurrentVersionToReturn = "8.1.5",
            ReturnStdOut = ""
        };

        var exitCode = command.Execute();

        exitCode.Should().Be(0);
        command.Results.Should().ContainSingle(r =>
            r.result.Status == KifaActionStatus.Skipped &&
            r.result.Message == "Tool 'Kifa.Tools.FileUtil' (version '8.1.5') is already installed.");
    }

    [Fact]
    public void Execute_WithOptions_AppendsVersionAndPrerelease() {
        var command = new TestUpgradeCommand {
            PackageIdToReturn = "Kifa.Tools.YoutubeUtil",
            Version = "8.0.0",
            Prerelease = true
        };

        var exitCode = command.Execute();

        exitCode.Should().Be(0);
        command.ExecutedCommands.Should().ContainSingle()
            .Which.Should().Be("tool update -g Kifa.Tools.YoutubeUtil --version 8.0.0 --prerelease");
    }

    [Fact]
    public void Execute_UnknownPackageId_ReturnsFailureExitCode() {
        var command = new TestUpgradeCommand {
            PackageIdToReturn = null
        };

        var exitCode = command.Execute();

        exitCode.Should().Be(1);
        command.ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public void Execute_CommandFails_ReturnsFailureExitCode() {
        var command = new TestUpgradeCommand {
            PackageIdToReturn = "Kifa.Tools.FileUtil",
            ReturnExitCode = 1,
            ReturnStdErr = "Failed to update tool."
        };

        var exitCode = command.Execute();

        exitCode.Should().Be(1);
        command.Results.Should().ContainSingle(r => r.result.Status == KifaActionStatus.Error);
    }
}
