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

        var calls = AgentTextCleaner.ExtractToolCalls(text, out var cleaned, out var dropped);

        cleaned.Should().Be("Let me check.Done.");
        dropped.Should().Be(0);
        calls.Should().ContainSingle();
        calls[0].Name.Should().Be("start_container");
        calls[0].Arguments.Should().ContainKey("id").WhoseValue.Should().Be("web");
    }

    [Fact]
    public void ExtractToolCalls_ShouldSupportArgumentsAsJsonString()
    {
        var text = "<｜DSML｜tool_call_start｜>{\"name\":\"start_container\",\"arguments\":\"{\\\"id\\\":\\\"web\\\"}\"}<｜DSML｜tool_call_end｜>";

        var calls = AgentTextCleaner.ExtractToolCalls(text, out _, out _);

        calls.Should().ContainSingle();
        calls[0].Arguments.Should().ContainKey("id").WhoseValue.Should().Be("web");
    }

    [Fact]
    public void ExtractToolCalls_ShouldDropBlocksWithMalformedJson()
    {
        var text = "<｜DSML｜tool_call_start｜>not json<｜DSML｜tool_call_end｜>Nothing here.";

        var calls = AgentTextCleaner.ExtractToolCalls(text, out var cleaned, out var dropped);

        calls.Should().BeEmpty();
        dropped.Should().Be(1);
        cleaned.Should().Be("Nothing here.");
    }

    [Fact]
    public void ExtractToolCalls_ShouldIgnoreStandaloneMarkers()
    {
        var text = "<｜DSML｜ etc";

        var calls = AgentTextCleaner.ExtractToolCalls(text, out var cleaned, out var dropped);

        calls.Should().BeEmpty();
        dropped.Should().Be(0);
        cleaned.Should().Be("<｜DSML｜ etc");
    }

    [Fact]
    public void HasUnclosedToolCallMarker_ShouldDetectCutOffToolCall()
    {
        AgentTextCleaner.HasUnclosedToolCallMarker("Still empty.<｜DSML｜tool_call_start｜>{\"name\":\"list_containers\"").Should().BeTrue();
        AgentTextCleaner.HasUnclosedToolCallMarker("Done.<｜DSML｜tool_call_start｜>{\"name\":\"list_containers\"}<｜DSML｜tool_call_end｜>").Should().BeFalse();
        AgentTextCleaner.HasUnclosedToolCallMarker("Plain text.").Should().BeFalse();
        AgentTextCleaner.HasUnclosedToolCallMarker("").Should().BeFalse();
    }

    [Theory]
    [InlineData("Let me test all the candidate addresses from inside the container to find which one actually works:", true)]
    [InlineData("Still empty. Let me check the latest execution error:", true)]
    [InlineData("I'll check the logs first.", true)]
    [InlineData("Let me explain what happened.", true)]
    [InlineData("No containers need changes.", false)]
    [InlineData("The working address is 10.0.0.5.", false)]
    [InlineData("You can find the address by running curl inside the container.", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsNarrationOnlyIncomplete_ShouldDetectAnnouncementWithoutAction(string input, bool expected)
    {
        AgentTextCleaner.IsNarrationOnlyIncomplete(input).Should().Be(expected);
    }
}
