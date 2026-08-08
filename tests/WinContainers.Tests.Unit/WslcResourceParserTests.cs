using FluentAssertions;
using WinContainers.Runtime;

namespace WinContainers.Tests.Unit;

public class WslcResourceParserTests
{
    [Fact]
    public void ParseVolumes_ShouldHandleJsonArray_OnSingleLine()
    {
        var output = @"[{""Driver"":""guest"",""Name"":""stirling_data""},{""Driver"":""guest"",""Name"":""n8n_data""}]";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Should().HaveCount(2);
        volumes[0].Name.Should().Be("stirling_data");
        volumes[0].Details.Should().Contain("guest");
        volumes[1].Name.Should().Be("n8n_data");
    }

    [Fact]
    public void ParseVolumes_ShouldHandleJsonLines()
    {
        var output = @"{""Driver"":""guest"",""Name"":""stirling_data""}" + "\n" + @"{""Driver"":""guest"",""Name"":""n8n_data""}";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Should().HaveCount(2);
        volumes[0].Name.Should().Be("stirling_data");
        volumes[1].Name.Should().Be("n8n_data");
    }

    [Fact]
    public void ParseVolumes_ShouldNotShowBrackets_WhenArrayHasMultipleEntries()
    {
        var output = @"[{""Driver"":""guest"",""Name"":""webchat_html""}]";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Should().HaveCount(1);
        volumes[0].Name.Should().NotContain("[");
        volumes[0].Name.Should().NotContain("]");
        volumes[0].Details.Should().NotContain("[");
        volumes[0].Details.Should().NotContain("]");
    }

    [Fact]
    public void ParseNetworks_ShouldHandleJsonArray_OnSingleLine()
    {
        var output = @"[{""ID"":""abc"",""Name"":""bridge"",""Driver"":""bridge"",""Scope"":""local""},{""ID"":""def"",""Name"":""mynet"",""Driver"":""bridge"",""Scope"":""local""}]";

        var networks = WslcResourceParser.ParseNetworks(output);

        networks.Should().HaveCount(2);
        networks[0].Name.Should().Be("bridge");
        networks[0].CanDelete.Should().BeFalse();
        networks[1].Name.Should().Be("mynet");
        networks[1].CanDelete.Should().BeTrue();
    }

    [Fact]
    public void ParseVolumes_ShouldFallbackToTable_WhenOutputIsNotJson()
    {
        var output = "DRIVER  VOLUME NAME\nguest   my_data\nguest   other_data";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Should().HaveCount(2);
        volumes[0].Name.Should().Be("my_data");
        volumes[1].Name.Should().Be("other_data");
    }

    [Fact]
    public void ParseVolumes_ShouldReturnEmpty_WhenOutputIsEmpty()
    {
        WslcResourceParser.ParseVolumes("").Should().BeEmpty();
        WslcResourceParser.ParseVolumes(null).Should().BeEmpty();
    }

    [Fact]
    public void ParseVolumes_ShouldSkipEntriesWithoutName()
    {
        var output = @"[{""Driver"":""guest""},{""Driver"":""guest"",""Name"":""n8n_data""}]";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Should().HaveCount(1);
        volumes[0].Name.Should().Be("n8n_data");
    }
}
