using FluentAssertions;
using WinContainers.Runtime;

namespace WinContainers.Tests.Unit;

public sealed class PortBindingConverterTests
{
    [Theory]
    [InlineData("8080:80/tcp", false, "127.0.0.1:8080:80/tcp")]
    [InlineData("127.0.0.1:8080->80/tcp", false, "127.0.0.1:8080:80/tcp")]
    [InlineData("0.0.0.0:8080->80/tcp", true, "0.0.0.0:8080:80/tcp")]
    public void Convert_ShouldNormalizeHostBinding(string binding, bool allowLan, string expected)
    {
        var result = PortBindingConverter.Convert(binding, allowLan);

        result.Success.Should().BeTrue();
        result.Bindings.Should().Equal(expected);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Convert_ShouldPreserveCommaSeparatedPortDetails()
    {
        var result = PortBindingConverter.Convert(
            "8080:80/tcp, 5353:53/udp, 9000:9000",
            allowLocalNetworkAccess: true);

        result.Success.Should().BeTrue();
        result.Bindings.Should().Equal(
            "0.0.0.0:8080:80/tcp",
            "0.0.0.0:5353:53/udp",
            "0.0.0.0:9000:9000");
    }

    [Theory]
    [InlineData("")]
    [InlineData("8080")]
    [InlineData("8080:")]
    [InlineData("70000:80/tcp")]
    [InlineData("8080:0/tcp")]
    [InlineData("192.168.1.5:8080->80/tcp")]
    public void Convert_ShouldRejectInvalidBindings(string binding)
    {
        var result = PortBindingConverter.Convert(binding, allowLocalNetworkAccess: true);

        result.Success.Should().BeFalse();
        result.Bindings.Should().BeEmpty();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
