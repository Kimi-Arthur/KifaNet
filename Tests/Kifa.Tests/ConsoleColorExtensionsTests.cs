using FluentAssertions;
using Xunit;

namespace Kifa.Tests;

public class ConsoleColorExtensionsTests {
    [Fact]
    public void ConsoleColorExtensions_NullOrEmptyString() {
        ConsoleColorExtensions.ForceEnabled = true;
        try {
            string? nullStr = null;
            nullStr.Info().Should().BeNull();
            "".Info().Should().Be("");
        } finally {
            ConsoleColorExtensions.ForceEnabled = null;
        }
    }

    [Fact]
    public void ConsoleColorExtensions_Disabled() {
        ConsoleColorExtensions.ForceEnabled = false;
        try {
            "hello".Info().Should().Be("hello");
            "hello".Warn().Should().Be("hello");
        } finally {
            ConsoleColorExtensions.ForceEnabled = null;
        }
    }

    [Fact]
    public void ConsoleColorExtensions_PredefinedMethods() {
        ConsoleColorExtensions.ForceEnabled = true;
        try {
            "msg".Trace().Should().Be("\u001b[37mmsg\u001b[0m");
            "msg".Debug().Should().Be("\u001b[97mmsg\u001b[0m");
            "msg".Info().Should().Be("\u001b[32mmsg\u001b[0m");
            "msg".Warn().Should().Be("\u001b[33mmsg\u001b[0m");
            "msg".Error().Should().Be("\u001b[35mmsg\u001b[0m");
            "msg".Fatal().Should().Be("\u001b[31mmsg\u001b[0m");
        } finally {
            ConsoleColorExtensions.ForceEnabled = null;
        }
    }

    [Fact]
    public void ConsoleColorExtensions_NestedColors() {
        ConsoleColorExtensions.ForceEnabled = true;
        try {
            var inner = "warn".Warn();
            var outer = $"[{inner}]".Info();
            outer.Should().Be("\u001b[32m[\u001b[33mwarn\u001b[0m\u001b[32m]\u001b[0m");
        } finally {
            ConsoleColorExtensions.ForceEnabled = null;
        }
    }
}
