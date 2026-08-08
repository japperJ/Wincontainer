using FluentAssertions;
using WinContainers.AI;

namespace WinContainers.Tests.Unit.Ai;

public class AgentTextCleanerTests
{
    [Theory]
    [InlineData("<｜DSML｜ etc", "")]
    [InlineData("<｜DSML｜tool_call_start｜>{\"name\":\"list_containers\"}<｜DSML｜tool_call_end｜>Done.", "Done.")]
    [InlineData("All containers are healthy.", "All containers are healthy.")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void StripSpecialTokens_ShouldRemoveDsmlMarkers(string input, string expected)
    {
        AgentTextCleaner.StripSpecialTokens(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("<｜DSML｜ etc", "<｜DSML｜ etc")]
    [InlineData("<｜DSML｜tool_call_start｜>{\"name\":\"list_containers\"}<｜DSML｜tool_call_end｜>Done.", "Done.")]
    [InlineData("All containers are healthy.", "All containers are healthy.")]
    [InlineData("<｜DSML｜tool_call_start｜>{\"name\":\"list", "<｜DSML｜tool_call_start｜>{\"name\":\"list")]
    public void SanitizeStreaming_ShouldKeepIncompleteMarkers_ButRemoveCompleteBlocks(string input, string expected)
    {
        AgentTextCleaner.SanitizeStreaming(input).Should().Be(expected);
    }

    [Fact]
    public void ExtractToolCalls_ShouldReturnParsedCalls_AndCleanText()
    {
        var text = "Let me check.<｜DSML｜tool_call_start｜>{\"name\":\"start_container\",\"arguments\":{\"id\":\"web\"}}<｜DSML｜tool_call_end｜>Done.";

        var calls = AgentTextCleaner.ExtractToolCalls(text, out var cleaned);

        cleaned.Should().Be("Let me check.Done.");
        calls.Should().ContainSingle();
        calls[0].Name.Should().Be("start_container");
        calls[0].Arguments.Should().ContainKey("id").WhoseValue.Should().Be("web");
    }

    [Fact]
    public void ExtractToolCalls_ShouldSupportArgumentsAsJsonString()
    {
        var text = "<｜DSML｜tool_call_start｜>{\"name\":\"start_container\",\"arguments\":\"{\\\"id\\\":\\\"web\\\"}\"}<｜DSML｜tool_call_end｜>";

        var calls = AgentTextCleaner.ExtractToolCalls(text, out _);

        calls.Should().ContainSingle();
        calls[0].Arguments.Should().ContainKey("id").WhoseValue.Should().Be("web");
    }

    [Fact]
    public void ExtractToolCalls_ShouldDropBlocksWithMalformedJson()
    {
        var text = "<｜DSML｜tool_call_start｜>not json<｜DSML｜tool_call_end｜>Nothing here.";

        var calls = AgentTextCleaner.ExtractToolCalls(text, out var cleaned);

        calls.Should().BeEmpty();
        cleaned.Should().Be("Nothing here.");
    }

    [Fact]
    public void ExtractToolCalls_ShouldIgnoreStandaloneMarkers()
    {
        var text = "<｜DSML｜ etc";

        var calls = AgentTextCleaner.ExtractToolCalls(text, out var cleaned);

        calls.Should().BeEmpty();
        cleaned.Should().Be("<｜DSML｜ etc");
    }
}
