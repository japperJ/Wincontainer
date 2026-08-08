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
}
