using FluentAssertions;

namespace WinContainers.Tests.Unit;

public sealed class ContainerAccessContractTests
{
    [Fact]
    public void ServiceHost_ShouldExposeAuthenticatedAccessRouteContract()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Service/Host/ServiceHost.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("MapPost(\"/api/containers/{id}/access\"");
        source.Should().Contain("request.ContainerId");
        source.Should().Contain("request.AllowLocalNetworkAccess");
        source.Should().Contain("ContainerAccessService");
    }

    [Fact]
    public void ClientAndDetailPage_ShouldWireAccessToggleAndCopyActions()
    {
        var clientPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Services/WslcServiceClient.cs"));
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/ContainerDetailPage.xaml"));
        var codeBehindPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs"));

        File.ReadAllText(clientPath).Should().Contain("/access");
        File.ReadAllText(clientPath).Should().Contain("allowLocalNetworkAccess");
        File.ReadAllText(xamlPath).Should().Contain("AccessToggle_Toggled");
        File.ReadAllText(xamlPath).Should().Contain("CopyAccessEndpointButton_Click");
        File.ReadAllText(codeBehindPath).Should().Contain("ConfirmLocalNetworkAccessAsync");
        File.ReadAllText(codeBehindPath).Should().Contain("Clipboard.SetContent");
    }
}
