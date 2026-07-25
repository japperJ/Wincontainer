using FluentAssertions;
using WinContainers.Core;

namespace WinContainers.Tests.Unit;

public sealed class ReleaseMetadataTests
{
    [Theory]
    [InlineData("v1.2.3")]
    [InlineData("v1.2.3-beta.1")]
    public void AcceptsSemVerTags(string tag)
    {
        ReleaseMetadata.IsValidTag(tag).Should().BeTrue();
    }

    [Theory]
    [InlineData("v1.2.3", "Stable", "1.2.3")]
    [InlineData("v1.2.3-beta.1", "Beta", "1.2.3-beta.1")]
    public void DerivesChannelAndVersion(string tag, string channel, string version)
    {
        ReleaseMetadata.GetChannel(tag).Should().Be(channel);
        ReleaseMetadata.GetVersion(tag).Should().Be(version);
    }

    [Fact]
    public void RejectsNonSemVerTags()
    {
        ReleaseMetadata.IsValidTag("release-1.2.3").Should().BeFalse();
        Action action = () => ReleaseMetadata.GetVersion("v1.2");
        action.Should().Throw<ArgumentException>();
    }
}
