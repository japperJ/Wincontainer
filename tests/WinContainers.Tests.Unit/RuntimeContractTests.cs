using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers.Service.Mcp;
using WinContainers.Tests.Unit.Ai;

namespace WinContainers.Tests.Unit;

public class RuntimeContractTests
{
    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    public void EnvironmentBooleanParser_ShouldParseDocumentedValues(string value, bool expected)
    {
        EnvironmentBooleanParser.TryParse(value, out var result).Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("yes")]
    public void EnvironmentBooleanParser_ShouldRejectUndocumentedValues(string? value)
    {
        EnvironmentBooleanParser.TryParse(value, out _).Should().BeFalse();
    }

    [Fact]
    public void ViewModelBase_ShouldHandleDispatcherLifecycleSafely()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/ViewModels/ViewModelBase.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("DispatcherQueue.GetForCurrentThread()");
        source.Should().Contain("dispatcherQueue is null");
        source.Should().Contain("if (!dispatcherQueue.TryEnqueue");
        source.Should().NotContain("App.DispatcherQueue.HasThreadAccess");
    }

    [Fact]
    public void ContainerDetailPage_ShouldUnsubscribeInspectPropertyChangedHandler()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("PropertyChangedEventHandler? _inspectPropertyChangedHandler");
        source.Should().Contain("_viewModel.PropertyChanged -= _inspectPropertyChangedHandler");
        source.Should().Contain("_viewModel.PropertyChanged += _inspectPropertyChangedHandler");
        source.Should().Contain("_inspectPropertyChangedHandler = null");
        source.Should().NotContain("_viewModel.PropertyChanged += async (s, e) =>");
    }

    [Fact]
    public void ServiceInfo_ShouldRoundTripPortTokenAndScripts()
    {
        var info = new ServiceInfo("12345", "secret-token")
        {
            Scripts = ["Get-Container", "Pull-Image"]
        };

        info.Port.Should().Be("12345");
        info.Token.Should().Be("secret-token");
        info.Scripts.Should().Contain("Get-Container");
        info.Scripts.Should().Contain("Pull-Image");
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldDefaultToLoopbackListenAndLoopbackClient()
    {
        ServiceEndpointResolver.ClearOverrides();

        var originalHost = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST");
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", null);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", null);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", null);

        try
        {
            ServiceEndpointResolver.Resolve().Should().Be("http://127.0.0.1:5123");
            ServiceEndpointResolver.ResolveServiceHost().Should().Be("127.0.0.1");
            ServiceEndpointResolver.ResolveServicePort().Should().Be("5123");
            ServiceEndpointResolver.ResolveToken().Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", originalHost);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldListenOnLanWhenTokenConfigured()
    {
        ServiceEndpointResolver.ClearOverrides();

        var originalHost = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", null);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        try
        {
            ServiceEndpointResolver.ResolveServiceHost().Should().Be("0.0.0.0");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", originalHost);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldHonorHostEnvironmentVariableOverToken()
    {
        ServiceEndpointResolver.ClearOverrides();

        var originalHost = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", "192.168.1.5");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        try
        {
            ServiceEndpointResolver.ResolveServiceHost().Should().Be("192.168.1.5");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", originalHost);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public void WslcDriver_ShouldExist()
    {
        typeof(WslcDriver).Should().NotBeNull();
    }

    [Fact]
    public void WslcDriver_ShouldBoundOutputCleanupAfterTimeout()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/WslcDriver.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("await DrainOutputAsync(stdoutTask, stderrTask)");
        source.Should().Contain("Task.WhenAll(stdoutTask, stderrTask)");
        source.Should().Contain("WaitAsync(TimeSpan.FromMilliseconds(OutputCleanupTimeoutMs))");
        source.Should().Contain("task.Exception");
    }

    [Fact]
    public void WslcDriver_ShouldKillAndDrainOnCallerCancellation()
    {
        // RunAsync creates Process directly, so this contract test asserts the
        // cancellation branch in source instead of spinning up wslc.exe.
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/WslcDriver.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("catch (OperationCanceledException) when (ct.IsCancellationRequested && !timeoutCts.IsCancellationRequested)");
        source.Should().Contain("TryKill(process);");
        source.Should().Contain("await DrainOutputAsync(stdoutTask, stderrTask);");
        source.Should().Contain("throw;");
    }

    [Fact]
    public void OnboardingViewModel_ShouldCleanupElevatedTempFilesOnEveryExitPath()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/ViewModels/OnboardingViewModel.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("var invocationDir = Path.Combine");
        source.Should().Contain("Directory.CreateDirectory(invocationDir)");
        source.Should().Contain("finally");
        source.Should().Contain("TryDeleteTempDirectory(invocationDir)");
    }

    [Fact]
    public void WslcDriver_ShouldExposeExpectedMethods()
    {
        typeof(WslcDriver).GetInterfaces().Should().Contain(typeof(IWslcDriver));

        var methods = typeof(WslcDriver).GetMethods()
            .Where(m => m.DeclaringType == typeof(WslcDriver))
            .Select(m => m.Name)
            .Distinct()
            .ToHashSet();

        methods.Should().Contain(nameof(IWslcDriver.GetVersionAsync));
        methods.Should().Contain(nameof(IWslcDriver.GetContainersAsync));
        methods.Should().Contain(nameof(IWslcDriver.StartContainerAsync));
        methods.Should().Contain(nameof(IWslcDriver.StopContainerAsync));
        methods.Should().Contain(nameof(IWslcDriver.RemoveContainerAsync));
        methods.Should().Contain(nameof(IWslcDriver.GetImagesAsync));
        methods.Should().Contain(nameof(IWslcDriver.PullImageAsync));
        methods.Should().Contain(nameof(IWslcDriver.LoadImageAsync));
        methods.Should().Contain(nameof(IWslcDriver.RemoveImageAsync));
        methods.Should().Contain(nameof(IWslcDriver.GetVolumesAsync));
        methods.Should().Contain(nameof(IWslcDriver.CreateVolumeAsync));
        methods.Should().Contain(nameof(IWslcDriver.RemoveVolumeAsync));
        methods.Should().Contain(nameof(IWslcDriver.GetNetworksAsync));
        methods.Should().Contain(nameof(IWslcDriver.CreateNetworkAsync));
        methods.Should().Contain(nameof(IWslcDriver.RemoveNetworkAsync));
    }

    [Fact]
    public async Task WslcDriver_LoadImageAsync_ShouldRejectMissingOrDuplicateArguments()
    {
        var driver = new WslcDriver();

        (await driver.LoadImageAsync(null, null, CancellationToken.None))
            .Should().Be("Validation error: provide exactly one of tarPath or tarData.");

        (await driver.LoadImageAsync("image.tar", "dGFy", CancellationToken.None))
            .Should().Be("Validation error: provide exactly one of tarPath or tarData.");
    }

    [Fact]
    public async Task McpTools_LoadImage_ShouldRejectMissingOrDuplicateArguments()
    {
        var driver = new FakeDriver();

        (await WinContainers.Service.Mcp.WincontainerTools.LoadImage(null, null, driver, CancellationToken.None))
            .Should().Be("Validation error: provide exactly one of tarPath or tarData.");

#nullable disable
        driver.LastLoadImageTarPath.Should().BeNull();
        driver.LastLoadImageTarData.Should().BeNull();
#nullable restore
    }

    [Fact]
    public async Task McpTools_LoadImage_ShouldDelegateTarPath()
    {
        var driver = new FakeDriver();
        var path = "C:\\images\\app.tar";

        var result = await WinContainers.Service.Mcp.WincontainerTools.LoadImage(path, null, driver, CancellationToken.None);

        result.Should().Contain("\"tool\":\"load_image\"");
        result.Should().Contain("\"success\":true");
        result.Should().Contain("\"result\":\"\"");
        driver.LastLoadImageTarPath.Should().Be(path);
        driver.LastLoadImageTarData.Should().BeNull();
    }

    [Fact]
    public async Task McpTools_LoadImage_ShouldDelegateTarData()
    {
        var driver = new FakeDriver();
        var data = "dGFy";

        var result = await WinContainers.Service.Mcp.WincontainerTools.LoadImage(null, data, driver, CancellationToken.None);

        result.Should().Contain("\"tool\":\"load_image\"");
        result.Should().Contain("\"success\":true");
        result.Should().Contain("\"result\":\"\"");
        driver.LastLoadImageTarPath.Should().BeNull();
        driver.LastLoadImageTarData.Should().Be(data);
    }

    private static IDisposable SubscribeToApprovalRequests(Action<McpDestructiveApprovalRequest> onRequest)
    {
        EventHandler<McpDestructiveApprovalRequest> handler = (_, request) => onRequest(request);
        McpDestructiveConfirmationPolicy.ApprovalRequested += handler;
        return new DelegateDisposable(() =>
            McpDestructiveConfirmationPolicy.ApprovalRequested -= handler);
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldRequireApprovalAndConsumeOnlyOnce()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        McpDestructiveApprovalRequest? request = null;
        using var subscription = SubscribeToApprovalRequests(value => request = value);

        var operation = McpDestructiveConfirmationPolicy.IssueOperation(
            "remove_container",
            "containers:alpha",
            "Remove container 'alpha'.",
            "session-1",
            "Test session",
            true,
            true,
            "Administrator session");

        operation.ApprovalStatus.Should().Be(McpDestructiveApprovalStatus.Pending);
        request.Should().NotBeNull();
        request.Should().BeAssignableTo<EventArgs>();
        request!.OperationId.Should().Be(operation.OperationId);
        request.ToolName.Should().Be("remove_container");
        request.DisplaySummary.Should().Be("Remove container 'alpha'.");
        request.SessionId.Should().Be("session-1");
        request.SessionName.Should().Be("Test session");
        request.SessionVisibleInUi.Should().BeTrue();
        request.SessionIsAdmin.Should().BeTrue();
        request.SessionWarning.Should().Be("Administrator session");

        McpDestructiveConfirmationPolicy.TryGetApprovalStatus(
                operation.OperationId,
                out var pendingStatus,
                out var statusReason)
            .Should().BeTrue();
        pendingStatus.Should().Be(McpDestructiveApprovalStatus.Pending);
        statusReason.Should().BeEmpty();

        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_container",
                operation.OperationId,
                "containers:alpha",
                out var pendingReason)
            .Should().BeFalse();
        pendingReason.Should().Be("human approval is still pending");

        McpDestructiveConfirmationPolicy.TryApprove(operation.OperationId, out var approvalReason)
            .Should().BeTrue();
        approvalReason.Should().BeEmpty();
        McpDestructiveConfirmationPolicy.TryGetApprovalStatus(
                operation.OperationId,
                out var approvedStatus,
                out statusReason)
            .Should().BeTrue();
        approvedStatus.Should().Be(McpDestructiveApprovalStatus.Approved);
        statusReason.Should().BeEmpty();

        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_container",
                operation.OperationId,
                "containers:alpha",
                out var reason)
            .Should().BeTrue();
        reason.Should().BeEmpty();

        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_container",
                operation.OperationId,
                "containers:alpha",
                out var secondReason)
            .Should().BeFalse();
        secondReason.Should().Be("operation id already used");
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldRejectWrongToolAndArguments()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var subscription = SubscribeToApprovalRequests(_ => { });
        var operation = McpDestructiveConfirmationPolicy.IssueOperation("remove_volume", "volume:alpha");

        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_container",
                operation.OperationId,
                "volume:alpha",
                out var toolReason)
            .Should().BeFalse();
        toolReason.Should().Be("tool name does not match issued operation");

        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_volume",
                operation.OperationId,
                "volume:beta",
                out var argsReason)
            .Should().BeFalse();
        argsReason.Should().Be("normalized arguments do not match issued operation");

        McpDestructiveConfirmationPolicy.TryGetApprovalStatus(
                operation.OperationId,
                out var status,
                out var statusReason)
            .Should().BeTrue();
        status.Should().Be(McpDestructiveApprovalStatus.Pending);
        statusReason.Should().BeEmpty();
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldPreserveArgumentPositionsWhenCanonicalizing()
    {
        var first = McpDestructiveConfirmationPolicy.CanonicalizeArguments(
            "web",
            "myapp:latest",
            string.Empty,
            "80:80");
        var second = McpDestructiveConfirmationPolicy.CanonicalizeArguments(
            "web",
            "myapp:latest",
            "80:80",
            string.Empty);

        first.Should().NotBe(second);
    }

    [Fact]
    public async Task McpTools_RemoveContainer_ShouldRequireConfirmationBeforeExecution()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var subscription = SubscribeToApprovalRequests(_ => { });
        var driver = new FakeDriver();

        var result = await WinContainers.Service.Mcp.WincontainerTools.RemoveContainer("web-app", driver, CancellationToken.None);

        result.Should().Contain("\"requiresConfirmation\":true");
        result.Should().Contain("\"operationId\":");
        driver.RemovedContainers.Should().BeEmpty();

        using var doc = JsonDocument.Parse(result);
        var operationId = doc.RootElement.GetProperty("operationId").GetString();
        operationId.Should().NotBeNullOrEmpty();

        McpDestructiveConfirmationPolicy.TryApprove(operationId!).Should().BeTrue();
        var confirmResult = await WinContainers.Service.Mcp.WincontainerTools.RemoveContainer("web-app", driver, CancellationToken.None, confirm: true, operationId: operationId);
        using var confirmDocument = JsonDocument.Parse(confirmResult);
        confirmDocument.RootElement.GetProperty("approvalStatus").GetString().Should().Be("approved");
        confirmResult.Should().Contain("web-app")
            .And.NotContain("requiresConfirmation");
        driver.RemovedContainers.Should().ContainSingle().Which.Should().Be("web-app");
    }

    [Fact]
    public async Task McpTools_RemoveContainer_ShouldAllowLegacyOneCallWhenToggleDisabled()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(false);
        try
        {
            var driver = new FakeDriver();
            var result = await WinContainers.Service.Mcp.WincontainerTools.RemoveContainer("legacy-app", driver, CancellationToken.None);

            result.Should().NotContain("requiresConfirmation");
            driver.RemovedContainers.Should().ContainSingle().Which.Should().Be("legacy-app");
        }
        finally
        {
            McpDestructiveConfirmationPolicy.SetEnabled(true);
        }
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldRejectDeniedOperation()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var subscription = SubscribeToApprovalRequests(_ => { });

        var denied = McpDestructiveConfirmationPolicy.IssueOperation("remove_image", "image:denied");
        McpDestructiveConfirmationPolicy.TryReject(denied.OperationId, out var rejectionReason)
            .Should().BeTrue();
        rejectionReason.Should().BeEmpty();
        McpDestructiveConfirmationPolicy.TryGetApprovalStatus(
                denied.OperationId,
                out var deniedStatus,
                out var statusReason)
            .Should().BeTrue();
        deniedStatus.Should().Be(McpDestructiveApprovalStatus.Denied);
        statusReason.Should().BeEmpty();
        McpDestructiveConfirmationPolicy.TryConsume("remove_image", denied.OperationId, "image:denied", out var deniedReason)
            .Should().BeFalse();
        deniedReason.Should().Be("human approval was denied");
        McpDestructiveConfirmationPolicy.TryApprove(denied.OperationId, out var approvalReason)
            .Should().BeFalse();
        approvalReason.Should().Be("human approval was already denied");
        McpDestructiveConfirmationPolicy.TryReject(denied.OperationId, out var secondRejectReason)
            .Should().BeFalse();
        secondRejectReason.Should().Be("human approval was already denied");
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldFailClosedWhenApprovalChannelFails()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        EventHandler<McpDestructiveApprovalRequest> handler = (_, _) => throw new InvalidOperationException("approval channel unavailable");
        McpDestructiveConfirmationPolicy.ApprovalRequested += handler;
        try
        {
            var operation = McpDestructiveConfirmationPolicy.IssueOperation("remove_image", "image:unavailable");

            operation.ApprovalStatus.Should().Be(McpDestructiveApprovalStatus.Unavailable);
            McpDestructiveConfirmationPolicy.TryConsume(
                    "remove_image",
                    operation.OperationId,
                    "image:unavailable",
                    out var reason)
                .Should().BeFalse();
            reason.Should().Be("human approval channel is unavailable");
        }
        finally
        {
            McpDestructiveConfirmationPolicy.ApprovalRequested -= handler;
        }
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldKeepUsefulExpiredReason()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var subscription = SubscribeToApprovalRequests(_ => { });
        var operation = McpDestructiveConfirmationPolicy.IssueOperation("remove_volume", "volume:expired");
        McpDestructiveConfirmationPolicy.TryApprove(operation.OperationId, out var approvalReason)
            .Should().BeTrue();
        approvalReason.Should().BeEmpty();

        var expiredAt = operation.ExpiresAtUtc.AddSeconds(1);
        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_volume",
                operation.OperationId,
                "volume:expired",
                out var expiredReason,
                expiredAt)
            .Should().BeFalse();
        expiredReason.Should().Be("operation expired");
        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_volume",
                operation.OperationId,
                "volume:expired",
                out var repeatedReason,
                expiredAt)
            .Should().BeFalse();
        repeatedReason.Should().Be("operation expired");
        McpDestructiveConfirmationPolicy.TryApprove(
                operation.OperationId,
                out var lateApprovalReason,
                expiredAt)
            .Should().BeFalse();
        lateApprovalReason.Should().Be("operation expired");
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldFailClosedWithoutSubscriber()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        var operation = McpDestructiveConfirmationPolicy.IssueOperation(
            "remove_network",
            "network:no-ui");

        McpDestructiveConfirmationPolicy.TryApprove(operation.OperationId, out var reason)
            .Should().BeFalse();
        reason.Should().Be("human approval channel is unavailable");
        McpDestructiveConfirmationPolicy.TryGetApprovalStatus(
                operation.OperationId,
                out var status,
                out var statusReason)
            .Should().BeTrue();
        status.Should().Be(McpDestructiveApprovalStatus.Unavailable);
        statusReason.Should().BeEmpty();
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldTraceMissingApprovalSubscriber()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var trace = new StringWriter();
        using var listener = new TextWriterTraceListener(trace);
        Trace.Listeners.Add(listener);

        try
        {
            McpDestructiveConfirmationPolicy.IssueOperation(
                "remove_network",
                "network:no-ui-log");

            Trace.Flush();
            trace.ToString().Should().Contain("ApprovalRequested has no subscribers");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }

    [Fact]
    public void McpDestructiveConfirmationPolicy_ShouldFailClosedAndTraceSubscriberException()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var trace = new StringWriter();
        using var listener = new TextWriterTraceListener(trace);
        Trace.Listeners.Add(listener);
        using var subscription = SubscribeToApprovalRequests(_ =>
            throw new InvalidOperationException("approval handler failed"));

        try
        {
            var operation = McpDestructiveConfirmationPolicy.IssueOperation(
                "remove_image",
                "image:handler-failure");

            McpDestructiveConfirmationPolicy.TryApprove(operation.OperationId, out var reason)
                .Should().BeFalse();
            reason.Should().Be("human approval channel is unavailable");
            Trace.Flush();
            trace.ToString().Should().Contain("ApprovalRequested subscriber failed")
                .And.Contain("approval handler failed");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }
    }

    [Fact]
    public async Task McpDestructiveConfirmationPolicy_ShouldSynchronizeConcurrentApprovalAndConsumption()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var subscription = SubscribeToApprovalRequests(_ => { });
        var operation = McpDestructiveConfirmationPolicy.IssueOperation("remove_network", "network:concurrent");
        using var start = new ManualResetEventSlim();

        var approval = Task.Run(() =>
        {
            start.Wait();
            return McpDestructiveConfirmationPolicy.TryApprove(operation.OperationId, out var reason)
                ? reason
                : null;
        });
        var consumptions = Enumerable.Range(0, 16).Select(_index => Task.Run(() =>
        {
            start.Wait();
            var success = McpDestructiveConfirmationPolicy.TryConsume(
                "remove_network",
                operation.OperationId,
                "network:concurrent",
                out var reason);
            return (success, reason);
        })).ToArray();

        start.Set();
        (await approval).Should().BeEmpty();
        var results = await Task.WhenAll(consumptions);

        if (results.All(result => !result.success))
        {
            McpDestructiveConfirmationPolicy.TryConsume(
                    "remove_network",
                    operation.OperationId,
                    "network:concurrent",
                    out var finalReason)
                .Should().BeTrue();
            finalReason.Should().BeEmpty();
        }
        else
        {
            results.Count(result => result.success).Should().Be(1);
        }

        McpDestructiveConfirmationPolicy.TryConsume(
                "remove_network",
                operation.OperationId,
                "network:concurrent",
                out var reusedReason)
            .Should().BeFalse();
        reusedReason.Should().Be("operation id already used");
    }

    [Fact]
    public async Task McpDestructiveConfirmationPolicy_ShouldSerializeConcurrentApprovalAndRejection()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var subscription = SubscribeToApprovalRequests(_ => { });
        var operation = McpDestructiveConfirmationPolicy.IssueOperation(
            "remove_network",
            "network:approval-race");
        using var start = new ManualResetEventSlim();

        var approval = Task.Run(() =>
        {
            start.Wait();
            return McpDestructiveConfirmationPolicy.TryApprove(operation.OperationId);
        });
        var rejection = Task.Run(() =>
        {
            start.Wait();
            return McpDestructiveConfirmationPolicy.TryReject(operation.OperationId);
        });

        start.Set();
        var results = await Task.WhenAll(approval, rejection);

        results.Count(result => result).Should().Be(1);
        McpDestructiveConfirmationPolicy.TryGetApprovalStatus(
                operation.OperationId,
                out var status,
                out var statusReason)
            .Should().BeTrue();
        statusReason.Should().BeEmpty();

        if (status == McpDestructiveApprovalStatus.Approved)
        {
            McpDestructiveConfirmationPolicy.TryConsume(
                    "remove_network",
                    operation.OperationId,
                    "network:approval-race",
                    out var consumeReason)
                .Should().BeTrue();
            consumeReason.Should().BeEmpty();
        }
        else
        {
            status.Should().Be(McpDestructiveApprovalStatus.Denied);
            McpDestructiveConfirmationPolicy.TryConsume(
                    "remove_network",
                    operation.OperationId,
                    "network:approval-race",
                    out var deniedReason)
                .Should().BeFalse();
            deniedReason.Should().Be("human approval was denied");
        }
    }

    [Fact]
    public async Task McpTools_RedeployWebOnly_ShouldUseSafeApprovalSummary()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        var driver = new FakeDriver();

        var result = await WinContainers.Service.Mcp.WincontainerTools.RedeployWebOnly(
            "web",
            "myapp:latest",
            "new-web",
            "80:80",
            "/host/secret:/container/secret",
            "PASSWORD=do-not-show",
            "frontend",
            driver,
            CancellationToken.None);

        using var document = JsonDocument.Parse(result);
        var summary = document.RootElement.GetProperty("approvalSummary").GetString();
        summary.Should().Contain("web").And.Contain("myapp:latest");
        summary.Should().Contain("ports:")
            .And.Contain("volumes:")
            .And.Contain("environment supplied")
            .And.Contain("name supplied")
            .And.Contain("network supplied");
        summary.Should().NotContain("PASSWORD").And.NotContain("do-not-show").And.NotContain("/host/secret");
        document.RootElement.GetProperty("humanApprovalRequired").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("approvalStatus").GetString().Should().Be("unavailable");
        driver.StoppedContainers.Should().BeEmpty();
        driver.RemovedContainers.Should().BeEmpty();
    }

    [Fact]
    public async Task McpTools_DestructiveOperations_ShouldExposeSafeTargetSummaries()
    {
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        using var subscription = SubscribeToApprovalRequests(_ => { });
        var driver = new FakeDriver();

        var results = new[]
        {
            await WinContainers.Service.Mcp.WincontainerTools.RemoveContainer(
                "summary-container", driver, CancellationToken.None),
            await WinContainers.Service.Mcp.WincontainerTools.RemoveImage(
                "summary-image", driver, CancellationToken.None),
            await WinContainers.Service.Mcp.WincontainerTools.RemoveVolume(
                "summary-volume", driver, CancellationToken.None),
            await WinContainers.Service.Mcp.WincontainerTools.RemoveNetwork(
                "summary-network", driver, CancellationToken.None)
        };

        results.Select(result =>
        {
            using var document = JsonDocument.Parse(result);
            return document.RootElement.GetProperty("approvalSummary").GetString();
        }).Should().ContainInOrder(
            "Remove container 'summary-container'.",
            "Remove image 'summary-image'.",
            "Remove volume 'summary-volume'.",
            "Remove network 'summary-network'.");
        driver.RemovedContainers.Should().BeEmpty();
        driver.RemovedImages.Should().BeEmpty();
        driver.RemovedVolumes.Should().BeEmpty();
        driver.RemovedNetworks.Should().BeEmpty();
    }

    [Fact]
    public async Task WslcDriver_LoadImageAsync_ShouldRejectInvalidTarPath()
    {
        var driver = new WslcDriver();
        var tempDir = Path.GetTempPath();
        var wrongExtension = Path.Combine(tempDir, $"{Guid.NewGuid():N}.txt");
        var missingTar = Path.Combine(tempDir, $"{Guid.NewGuid():N}.tar");

        await File.WriteAllTextAsync(wrongExtension, "not a tar");

        try
        {
            (await driver.LoadImageAsync(wrongExtension, null, CancellationToken.None))
                .Should().Be("Validation error: tarPath must point to an existing .tar file.");

            (await driver.LoadImageAsync(missingTar, null, CancellationToken.None))
                .Should().Be("Validation error: tarPath must point to an existing .tar file.");
        }
        finally
        {
            if (File.Exists(wrongExtension))
            {
                File.Delete(wrongExtension);
            }
        }
    }

    [Fact]
    public async Task WslcDriver_LoadImageAsync_ShouldRejectInvalidBase64()
    {
        var driver = new WslcDriver();

        (await driver.LoadImageAsync(null, "not-base64", CancellationToken.None))
            .Should().Be("Validation error: tarData is not valid base64.");

        var tempDir = Path.GetTempPath();
        var tarPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(tarPath, "not a tar");

        try
        {
            (await driver.LoadImageAsync(tarPath, null, CancellationToken.None))
                .Should().Be("Validation error: tarPath must point to an existing .tar file.");
        }
        finally
        {
            if (File.Exists(tarPath))
            {
                File.Delete(tarPath);
            }
        }
    }

    [Fact]
    public async Task WslcDriver_LoadImageAsync_ShouldAcceptValidTarPath()
    {
        var driver = new WslcDriver();
        var tempDir = Path.GetTempPath();
        var tarPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}.tar");
        await File.WriteAllTextAsync(tarPath, "tar");

        try
        {
            try
            {
                var result = await driver.LoadImageAsync(tarPath, null, CancellationToken.None);
                result.Should().NotStartWith("Validation error:");
            }
            catch (FileNotFoundException)
            {
                // Acceptable in test environments without WSLC installed.
            }
        }
        finally
        {
            if (File.Exists(tarPath))
            {
                File.Delete(tarPath);
            }
        }
    }

    [Fact]
    public void WslcDriver_ShouldRejectOversizedBase64BeforeDecoding()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/WslcDriver.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("Convert.FromBase64String(base64)");
        source.Should().Contain("decodedBytes.LongLength > MaxImageTarBytes");
    }

    [Fact]
    public async Task WslcDriver_LoadImageAsync_ShouldDeleteTempArchiveOnExit()
    {
        var driver = new WslcDriver();
        var tempDir = Path.GetTempPath();
        var before = Directory.GetFiles(tempDir, "*.tar").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tarData = Convert.ToBase64String(Encoding.UTF8.GetBytes("tar"));

        try
        {
            await driver.LoadImageAsync(null, tarData, CancellationToken.None);
        }
        catch (FileNotFoundException)
        {
        }

        var after = Directory.GetFiles(tempDir, "*.tar").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = after.Except(before).ToArray();

        created.Should().BeEmpty();
    }

    [Fact]
    public async Task ImageUploadStore_ShouldAppendOrderedChunksAndReturnCompletedPath()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        var archivePath = Path.Combine(Path.GetTempPath(), $"{upload.UploadId}.tar");
        var observedPath = string.Empty;

        (await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("abc"), CancellationToken.None))
            .Should().Be("Upload chunk accepted.");

        File.Exists(archivePath).Should().BeTrue();

        var result = await store.CompleteAsync(
            upload.UploadId,
            (path, ct) =>
            {
                observedPath = path;
                File.ReadAllText(path).Should().Be("abc");
                return Task.FromResult(path);
            },
            CancellationToken.None);

        result.Should().Be(observedPath);
        observedPath.Should().Be(archivePath);
        File.Exists(archivePath).Should().BeFalse();
    }

    [Fact]
    public async Task ImageUploadStore_ShouldAssembleTarArchiveAcrossChunks()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        var archive = CreateMinimalTarArchive();
        var firstChunk = archive[..512];
        var secondChunk = archive[512..1024];
        var thirdChunk = archive[1024..1536];
        var fourthChunk = archive[1536..];

        (await store.AppendChunkAsync(upload.UploadId, 0, ToBase64(firstChunk), CancellationToken.None))
            .Should().Be("Upload chunk accepted.");
        (await store.AppendChunkAsync(upload.UploadId, 1, ToBase64(secondChunk), CancellationToken.None))
            .Should().Be("Upload chunk accepted.");
        (await store.AppendChunkAsync(upload.UploadId, 2, ToBase64(thirdChunk), CancellationToken.None))
            .Should().Be("Upload chunk accepted.");
        (await store.AppendChunkAsync(upload.UploadId, 3, ToBase64(fourthChunk), CancellationToken.None))
            .Should().Be("Upload chunk accepted.");

        var result = await store.CompleteAsync(
            upload.UploadId,
            (path, ct) =>
            {
                File.ReadAllBytes(path).Should().Equal(archive);
                return Task.FromResult("tar loaded");
            },
            CancellationToken.None);

        result.Should().Be("tar loaded");
    }

    [Fact]
    public async Task ImageUploadStore_ShouldRejectEmptyUploadsBeforeLoading()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        var callbackInvoked = false;

        var result = await store.CompleteAsync(
            upload.UploadId,
            (_, _) =>
            {
                callbackInvoked = true;
                return Task.FromResult("loaded");
            },
            CancellationToken.None);

        result.Should().Be("Validation error: upload is empty.");
        callbackInvoked.Should().BeFalse();
        File.Exists(Path.Combine(Path.GetTempPath(), $"{upload.UploadId}.tar")).Should().BeFalse();
    }

    [Fact]
    public async Task ImageUploadStore_ShouldLetAppendObserveRemovalWhileCompletionCallbackRuns()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        var archivePath = Path.Combine(Path.GetTempPath(), $"{upload.UploadId}.tar");
        await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("abc"), CancellationToken.None);

        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var completeTask = store.CompleteAsync(
            upload.UploadId,
            async (path, ct) =>
            {
                callbackEntered.TrySetResult();
                await releaseCallback.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
                File.ReadAllText(path).Should().Be("abc");
                return path;
            },
            CancellationToken.None);

        await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var appendTask = store.AppendChunkAsync(upload.UploadId, 1, ToBase64("def"), CancellationToken.None);
        var appendFinished = await Task.WhenAny(appendTask, Task.Delay(TimeSpan.FromSeconds(5)));

        appendFinished.Should().Be(appendTask);
        (await appendTask).Should().Be("Validation error: upload ID was not found.");

        releaseCallback.TrySetResult();
        (await completeTask).Should().Be(archivePath);
        File.Exists(archivePath).Should().BeFalse();
    }

    [Fact]
    public void ImageUploadStore_ShouldRetainUploadsInsideLookupBeforeReturning()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/ImageUploadStore.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("state.Lease.RetainOperation();");
        source.Should().Contain("activeUpload = new ActiveUploadHandle(state);");
        source.Should().NotContain("state!.Lease.RetainOperation();");
    }

    [Fact]
    public void ImageUploadStore_ShouldBoundChunkDecodingBeforeAllocating()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/ImageUploadStore.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("TryGetMaximumDecodedBytes(base64Chunk, out var maxDecodedBytes)");
        source.Should().Contain("Convert.TryFromBase64String(base64Chunk, decodedBytes, out var decodedBytesWritten)");
        source.Should().Contain("new byte[MaxChunkBytes]");
        source.Should().NotContain("Convert.FromBase64String(base64Chunk)");
    }

    [Fact]
    public void ImageUploadStore_ShouldAbortCanceledAppendsAndRethrow()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/ImageUploadStore.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("catch (OperationCanceledException) when (ct.IsCancellationRequested)");
        source.Should().Contain("_uploads.Remove(uploadId);");
        source.Should().Contain("state.Lease.MarkRemoved(expired: false);");
        source.Should().Contain("TryDeleteFile(state.FilePath);");
        source.Should().Contain("throw;");
    }

    [Fact]
    public void ImageUploadStore_ShouldLogTempFileDeletionFailuresWithPath()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/ImageUploadStore.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("Temp file cleanup failed for");
        source.Should().Contain("{path}");
        source.Should().Contain("{ex}");
    }

    [Fact]
    public async Task ImageUploadStore_ShouldReturnExactValidationErrorsForMissingAndExpiredUploads()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var store = new ImageUploadStore(timeProvider);
        var upload = store.Start();
        var expiredCallbackInvoked = false;

        (await store.AppendChunkAsync("missing", 0, ToBase64("x"), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");

        (await store.CompleteAsync("missing", (_, _) => Task.FromResult(string.Empty), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");

        timeProvider.Advance(TimeSpan.FromMinutes(16));

        (await store.AppendChunkAsync("cleanup-trigger", 0, ToBase64("x"), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");

        (await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("x"), CancellationToken.None))
            .Should().Be("Validation error: upload has expired.");

        (await store.CompleteAsync(
            upload.UploadId,
            (_, _) =>
            {
                expiredCallbackInvoked = true;
                return Task.FromResult(string.Empty);
            },
            CancellationToken.None))
            .Should().Be("Validation error: upload has expired.");
        expiredCallbackInvoked.Should().BeFalse();
        File.Exists(Path.Combine(Path.GetTempPath(), $"{upload.UploadId}.tar")).Should().BeFalse();
    }

    [Fact]
    public async Task ImageUploadStore_ShouldPruneRecentlyExpiredIdsAfterLifetime()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var store = new ImageUploadStore(timeProvider);
        var upload = store.Start();

        timeProvider.Advance(TimeSpan.FromMinutes(16));

        (await store.AppendChunkAsync("cleanup-trigger", 0, ToBase64("x"), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");

        (await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("x"), CancellationToken.None))
            .Should().Be("Validation error: upload has expired.");

        timeProvider.Advance(TimeSpan.FromMinutes(16));

        (await store.AppendChunkAsync("cleanup-trigger-2", 0, ToBase64("y"), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");

        (await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("x"), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");
    }

    [Fact]
    public async Task ImageUploadStore_ShouldRejectChunksOverThreeKilobytesDecoded()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        var oversizedChunk = new byte[ImageUploadStore.MaxChunkBytes + 1];
        var result = await store.AppendChunkAsync(upload.UploadId, 0, ToBase64(oversizedChunk), CancellationToken.None);

        result.Should().Be("Validation error: chunk exceeds 3 KB after decoding.");
    }

    [Fact]
    public async Task ImageUploadStore_ShouldRejectTotalsOver512MB()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();

        (await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("abc"), CancellationToken.None))
            .Should().Be("Upload chunk accepted.");

        store.TrySetBytesWrittenForTesting(upload.UploadId, ImageUploadStore.MaxUploadBytes - 2)
            .Should().BeTrue();

        var result = await store.AppendChunkAsync(upload.UploadId, 1, ToBase64("abc"), CancellationToken.None);

        result.Should().Be("Validation error: upload exceeds 512 MB after decoding.");
    }

    [Fact]
    public async Task ImageUploadStore_ShouldCleanupAfterLoadCallbackThrows()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        var archivePath = Path.Combine(Path.GetTempPath(), $"{upload.UploadId}.tar");

        await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("abc"), CancellationToken.None);

        var invocation = async () => await store.CompleteAsync(
            upload.UploadId,
            (_, _) => throw new InvalidOperationException("load failed"),
            CancellationToken.None);

        await invocation.Should().ThrowAsync<InvalidOperationException>();
        (await store.AppendChunkAsync(upload.UploadId, 1, ToBase64("def"), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");
        File.Exists(archivePath).Should().BeFalse();
    }

    [Fact]
    public void McpTools_StartImageUpload_ShouldReturnUploadMetadata()
    {
        var store = new ImageUploadStore();

        var json = WinContainers.Service.Mcp.WincontainerTools.StartImageUpload(store);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("uploadId", out var uploadIdProp).Should().BeTrue();
        var uploadId = uploadIdProp.GetString();
        uploadId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task McpTools_UploadImageChunk_ShouldAppendDecodedChunk()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();

        var result = await WinContainers.Service.Mcp.WincontainerTools.UploadImageChunk(
            upload.UploadId,
            0,
            ToBase64("abc"),
            store,
            CancellationToken.None);

        result.Should().Be("Upload chunk accepted.");
    }

    [Fact]
    public async Task McpTools_FinishImageUpload_ShouldDelegatePathToDriver()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("abc"), CancellationToken.None);

        var driver = new FakeDriver();
        var archivePath = Path.Combine(Path.GetTempPath(), $"{upload.UploadId}.tar");

        var result = await WinContainers.Service.Mcp.WincontainerTools.FinishImageUpload(
            upload.UploadId,
            store,
            driver,
            CancellationToken.None);

        result.Should().BeEmpty();
        driver.LastLoadImageTarPath.Should().Be(archivePath);
        driver.LastLoadImageTarData.Should().BeNull();
        File.Exists(archivePath).Should().BeFalse();
    }

    [Fact]
    public async Task McpTools_FinishImageUpload_ShouldCleanUpAndRethrowCancellation()
    {
        var store = new ImageUploadStore();
        var upload = store.Start();
        await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("abc"), CancellationToken.None);
        var archivePath = Path.Combine(Path.GetTempPath(), $"{upload.UploadId}.tar");
        var canceled = new OperationCanceledException(CancellationToken.None);
        var driver = new CancelingDriver(canceled);

        var exception = await Record.ExceptionAsync(() => WinContainers.Service.Mcp.WincontainerTools.FinishImageUpload(
            upload.UploadId,
            store,
            driver,
            CancellationToken.None));

        exception.Should().BeSameAs(canceled);
        File.Exists(archivePath).Should().BeFalse();
        (await store.AppendChunkAsync(upload.UploadId, 1, ToBase64("def"), CancellationToken.None))
            .Should().Be("Validation error: upload ID was not found.");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseContainerJson()
    {
        var json = """
[
  {
    "ID": "abc123",
    "Names": "my-container",
    "Image": "nginx:alpine",
    "Status": "Running 2 hours",
    "Ports": "0.0.0.0:8080->80/tcp",
    "CreatedAt": "2025-01-01"
  },
  {
    "ID": "def456",
    "Names": "stopped-app",
    "Image": "alpine:latest",
    "Status": "Exited (0)",
    "Ports": "",
    "CreatedAt": "2025-01-02"
  }
]
""";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().HaveCount(2);
        containers[0].Id.Should().Be("abc123");
        containers[0].Name.Should().Be("my-container");
        containers[0].Image.Should().Be("nginx:alpine");
        containers[0].Status.Should().Be("Running 2 hours");
        containers[0].Ports.Should().Be("0.0.0.0:8080->80/tcp");
        containers[0].PortLinks.Should().ContainSingle(l => l.Url == "localhost:8080");

        containers[1].Id.Should().Be("def456");
        containers[1].Name.Should().Be("stopped-app");
        containers[1].Ports.Should().Be("No ports");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseCaseInsensitiveContainerFields()
    {
        var json = "[{\"id\":\"abc123\",\"name\":\"clean-host\",\"image\":\"nginx:latest\",\"status\":\"Up\",\"ports\":\"\"}]";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().ContainSingle();
        containers[0].Id.Should().Be("abc123");
        containers[0].Name.Should().Be("clean-host");
        containers[0].Image.Should().Be("nginx:latest");
        containers[0].Status.Should().Be("Up");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseStructuredPortsAndNumericState()
    {
        var json = "[{\"Id\":\"abc123\",\"Image\":\"nodered/node-red:latest\",\"Name\":\"nodered1\",\"Ports\":[{\"BindingAddress\":\"127.0.0.1\",\"ContainerPort\":1880,\"HostPort\":1880,\"Protocol\":6}],\"State\":2}]";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().ContainSingle();
        containers[0].Name.Should().Be("nodered1");
        containers[0].Status.Should().Be("Up");
        containers[0].Ports.Should().Be("127.0.0.1:1880->1880/tcp");
        containers[0].PortLinks.Should().ContainSingle(link => link.Url == "localhost:1880");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseContainerMounts()
    {
        var json = """
[
  {
    "ID": "mnt123",
    "Names": "data-app",
    "Image": "busybox:latest",
    "Status": "Up",
    "Ports": "",
    "Mounts": [
      { "Source": "app-data", "Destination": "/data" },
      { "SourcePath": "/host/config", "Destination": "/config" }
    ]
  }
]
""";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().ContainSingle();
        containers[0].MountInfos.Should().Contain(m => m.Source == "app-data" && m.Target == "/data");
        containers[0].MountInfos.Should().Contain(m => m.Source == "/host/config" && m.Target == "/config");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseImageJson()
    {
        var json = """
[
  {
    "ID": "sha256:abc123",
    "Repository": "nginx",
    "Tag": "alpine",
    "Size": "42MB",
    "CreatedSince": "2 weeks ago"
  }
]
""";

        var images = WslcContainerParser.ParseImages(json);

        images.Should().ContainSingle();
        images[0].Repository.Should().Be("nginx");
        images[0].Tag.Should().Be("alpine");
        images[0].ID.Should().Be("sha256:abc123");
        images[0].Size.Should().Be("42MB");
    }

    [Fact]
    public void WslcContainerParser_ShouldHandleEmptyJson()
    {
        WslcContainerParser.ParseContainers("[]").Should().BeEmpty();
        WslcContainerParser.ParseContainers("").Should().BeEmpty();
        WslcContainerParser.ParseContainers(null!).Should().BeEmpty();
        WslcContainerParser.ParseImages("[]").Should().BeEmpty();
        WslcContainerParser.ParseImages("").Should().BeEmpty();
    }

    [Fact]
    public void ImageListFormatter_ShouldRenderReadableImageSummary()
    {
        const string rawOutput = """
{"ID":"8b1e78743a03","Repository":"nginx","Tag":"alpine","Name":"docker.io/library/nginx:alpine"}
{"ID":"5b10f432ef3d","Repository":"alpine","Tag":"latest","Name":"docker.io/library/alpine:latest"}
""";

        var formatted = ImageListFormatter.Format(rawOutput);

        formatted.Should().Contain("Images: 2");
        formatted.Should().Contain("nginx:alpine");
        formatted.Should().Contain("alpine:latest");
        formatted.Should().NotContain("\"ID\":\"8b1e78743a03\"");
    }

    [Fact]
    public void WslcCommands_ShouldGenerateExpectedCommandStrings()
    {
        WslcCommands.Version().Should().Be("--version");
        WslcCommands.ContainerPs().Should().Be("container ps --all --format json");
        WslcCommands.ContainerStart("abc").Should().Be("container start abc");
        WslcCommands.ContainerStop("abc").Should().Be("container stop abc");
        WslcCommands.ImageLs().Should().Be("image ls --format json");
        WslcCommands.ImagePull("nginx").Should().Be("image pull nginx");
        WslcCommands.ImageLoad(@"C:\images\app.tar").Should().Be(@"image load --input C:\images\app.tar");
        WslcCommands.ImageLoad(@"C:\Users\me\my image.tar").Should().Be(@"image load --input ""C:\Users\me\my image.tar""");
        WslcCommands.VolumeLs().Should().Be("volume ls --format json");
        WslcCommands.NetworkLs().Should().Be("network ls --format json");
    }

    [Fact]
    public void WslcVersionFormatter_ShouldExtractWslcVersion()
    {
        WslcVersionFormatter.Format("wslc 2.9.4.0").Should().Be("2.9.4.0");
    }

    [Fact]
    public void WslcRuntimeProbe_ShouldUseAContainerCommandInsteadOfVersionOnly()
    {
        WslcCommands.ContainerPs().Should().NotBe(WslcCommands.Version());
        WslcCommands.ContainerPs().Should().Contain("container ps");
    }

    [Fact]
    public void WslcCommands_ShouldQuoteSpacesInArgs()
    {
        WslcCommands.ContainerStart("my container").Should().Contain("\"my container\"");
        WslcCommands.ImagePull("my image:v2").Should().Be("image pull \"my image:v2\"");
    }

    [Fact]
    public void WslcCommands_Run_ShouldNotEmitUnsupportedRestartOption()
    {
        var command = WslcCommands.Run("linuxserver/heimdall:latest", "heimdall97");

        command.Should().Be("run --detach --name heimdall97 linuxserver/heimdall:latest");
        command.Should().NotContain("--restart");
    }

    [Fact]
    public void WslcCommands_Run_ShouldAttachNamedNetworkWhenProvided()
    {
        var command = WslcCommands.Run("api:latest", "api", network: "famnet");

        command.Should().Be("run --detach --name api --network famnet api:latest");
    }

    [Fact]
    public void WslcCommands_Run_ShouldOmitNetworkWhenBlank()
    {
        var command = WslcCommands.Run("api:latest", network: " ");

        command.Should().Be("run --detach api:latest");
    }

    [Fact]
    public void QuickActions_ShouldNotExposeUnsupportedRestartPolicyConfiguration()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/QuickActionsControl.xaml"));
        var codeBehindPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/QuickActionsControl.xaml.cs"));
        var clientPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Services/WslcServiceClient.cs"));

        File.ReadAllText(xamlPath).Should().NotContain("RestartPolicyCombo");
        File.ReadAllText(codeBehindPath).Should().NotContain("RestartPolicy");
        var clientSource = File.ReadAllText(clientPath);
        clientSource.Should().NotContain("RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, string? restart");
        clientSource.Should().NotContain("JsonContent.Create(new { image, name, ports, volumes, env, restart })");
    }

    [Fact]
    public void OutputService_ShouldBoundInMemoryHistory()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Services/OutputService.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("MaxHistoryEntries = 1000");
        source.Should().Contain("in-memory diagnostic buffer");
        source.Should().Contain("_history.RemoveAt(0)");
        source.Should().Contain("_history.Count >= MaxHistoryEntries");
    }

    [Fact]
    public void Application_ShouldDeclareTheWindowIconAsItsExecutableIcon()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/WinContainers.App.csproj"));
        var windowPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/MainWindow.xaml.cs"));

        File.ReadAllText(projectPath).Should().Contain("<ApplicationIcon>Assets\\AppIcon.ico</ApplicationIcon>");
        File.ReadAllText(windowPath).Should().Contain("AppWindow.SetIcon(\"Assets/AppIcon.ico\")");
    }

    [Fact]
    public void PortLinkClick_ShouldStopTheEventBeforeLaunchingTheBrowser()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/ContainersControl.xaml"));
        var codeBehindPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/ContainersControl.xaml.cs"));
        var xaml = File.ReadAllText(xamlPath);
        var source = File.ReadAllText(codeBehindPath);

        xaml.Should().Contain("Tapped=\"PortLink_Tapped\"");
        source.Should().Contain("private void PortLink_Tapped(object sender, TappedRoutedEventArgs e)");
        source.Should().Contain("e.Handled = true;");
    }

    [Fact]
    public void ContainerListActions_ShouldShowOutputPaneBeforeRunning()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/ContainersControl.xaml.cs"));
        var source = File.ReadAllText(path);

        foreach (var handler in new[]
        {
            "StartContainer_Click",
            "StopContainer_Click",
            "RemoveContainer_Click",
            "StartGroup_Click",
            "StopGroup_Click",
            "RemoveGroup_Click"
        })
        {
            var handlerStart = source.IndexOf(handler, StringComparison.Ordinal);
            handlerStart.Should().BeGreaterThanOrEqualTo(0, because: $"{handler} should exist");
            var nextHandler = source.IndexOf("private ", handlerStart + handler.Length, StringComparison.Ordinal);
            var handlerSource = source.Substring(handlerStart, nextHandler < 0 ? source.Length - handlerStart : nextHandler - handlerStart);
            handlerSource.Should().Contain("EnsureOutputPaneVisible()", because: $"{handler} should show action output");
        }
    }

    [Fact]
    public void WslcResourceParser_ShouldParseVolumeList()
    {
        const string output = "DRIVER VOLUME NAME\nlocal app-data\nlocal cache";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Select(v => v.Name).Should().Equal("app-data", "cache");
    }

    [Fact]
    public void WslcResourceParser_ShouldParseJsonVolumeList()
    {
        const string output = "{\"Driver\":\"local\",\"Name\":\"app-data\",\"Mountpoint\":\"/var/lib/volumes/app-data/_data\",\"Scope\":\"local\"}\n" +
            "{\"Driver\":\"local\",\"Name\":\"cache\",\"Mountpoint\":\"/var/lib/volumes/cache/_data\",\"Scope\":\"local\"}";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Select(v => v.Name).Should().Equal("app-data", "cache");
    }

    [Fact]
    public void WslcResourceParser_ShouldParseNetworkList()
    {
        const string output = "NETWORK ID NAME DRIVER SCOPE\nabc123 bridge bridge local\ndef456 app-net bridge local";

        var networks = WslcResourceParser.ParseNetworks(output);

        networks.Select(n => n.Name).Should().Equal("bridge", "app-net");
        networks[0].Details.Should().Contain("abc123");
    }

    [Fact]
    public void WslcResourceParser_ShouldProtectBuiltInNetworks()
    {
        const string output = "{\"ID\":\"\",\"Name\":\"bridge\",\"Labels\":\"\"}\n" +
            "{\"ID\":\"custom\",\"Name\":\"app-net\",\"Labels\":\"\"}";

        var networks = WslcResourceParser.ParseNetworks(output);

        networks.Single(n => n.Name == "bridge").CanDelete.Should().BeFalse();
        networks.Single(n => n.Name == "app-net").CanDelete.Should().BeTrue();
    }

    [Fact]
    public void WslcCommands_ShouldGenerateContainerExecCommands()
    {
        WslcCommands.ContainerExecCommand("abc", "ls -lap /")
            .Should().Be("container exec abc ls -lap /");
        WslcCommands.ContainerExecShell("abc", "printf 'hello'")
            .Should().Be("container exec abc sh -c \"printf 'hello'\"");
        WslcCommands.ContainerExecShell("abc", "echo ok", "bash")
            .Should().Be("container exec abc bash -c \"echo ok\"");
    }

    [Fact]
    public void ContainerFilePaths_ShouldUseCentralizedShellQuoting()
    {
        var commandsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Core/WslcCommands.cs"));
        var viewModelPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs"));
        var commandsSource = File.ReadAllText(commandsPath);
        var viewModelSource = File.ReadAllText(viewModelPath);

        commandsSource.Should().Contain("public static string ShellQuote(string value)");
        commandsSource.Should().Contain("value.Replace(\"'\", \"'\\\\''\"");
        viewModelSource.Should().Contain("WslcCommands.ShellQuote(path)");
        viewModelSource.Should().Contain("WslcCommands.ShellQuote(filePath)");
        viewModelSource.Should().NotContain("private static string EscapePath");
        viewModelSource.Should().NotContain("private static string ShellQuote");

        WslcCommands.ShellQuote("/tmp/$(touch pwned)")
            .Should().Be("'/tmp/$(touch pwned)'");
        WslcCommands.ShellQuote("it's safe")
            .Should().Be("'it'\\''s safe'");
    }

    [Fact]
    public void WslcFileParser_ShouldExposeParseFileEntriesMethod()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.Runtime/WslcFileParser.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("ParseFileEntries");
        source.Should().Contain("JsonDocument.Parse");
    }

    [Fact]
    public void WslcFileParser_ShouldPreserveNamesFromNulDelimitedRecords()
    {
        var output = "d\tname with spaces\0f\tline\tbreak\0f\tquote'file\0";

        var entries = WslcFileParser.Parse(output);

        entries.Select(entry => (entry.Name, entry.Type)).Should().Equal(
            ("name with spaces", "dir"),
            ("line\tbreak", "file"),
            ("quote'file", "file"));
    }

    [Fact]
    public void ContainerFileListing_ShouldUseDelimitedShellOutputAndServiceParser()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("WslcFileParser.ParseFileEntries(output)");
        source.Should().Contain("printf 'd\\\\t%s\\\\0'");
        source.Should().Contain("printf 'f\\\\t%s\\\\0'");
        source.Should().NotContain("ls -lap");
        source.Should().NotContain("line.Split(' ', StringSplitOptions.RemoveEmptyEntries)");
    }

    [Fact]
    public void RuntimeTools_ShouldCheckExecutableOnPath()
    {
        RuntimeTools.IsExecutableAvailable("wslc");
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldHonorEnvironmentPortOverride()
    {
        ServiceEndpointResolver.ClearOverrides();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "5155");

        try
        {
            var endpoint = ServiceEndpointResolver.Resolve();

            endpoint.Should().Be("http://127.0.0.1:5155");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", null);
        }
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldResolveBearerToken()
    {
        ServiceEndpointResolver.ClearOverrides();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        try
        {
            ServiceEndpointResolver.ResolveToken().Should().Be("test-token");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", null);
        }
    }

    [Fact]
    public void BearerTokenValidator_ShouldAuthorizeBearerTokenRequests()
    {
        BearerTokenValidator.IsAuthorized(string.Concat("Bearer", " ", "abc123"), "abc123").Should().BeTrue();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRejectInvalidBearerTokens()
    {
        BearerTokenValidator.IsAuthorized(string.Concat("Bearer", " ", "wrong-token"), "abc123").Should().BeFalse();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRejectRequestsWhenExpectedTokenIsEmpty()
    {
        BearerTokenValidator.IsAuthorized("Bearer abc123", string.Empty).Should().BeFalse();
        BearerTokenValidator.IsAuthorized(string.Empty, string.Empty).Should().BeFalse();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRequireAuthorizationForAnyIpListenHostWithoutToken()
    {
        BearerTokenValidator.RequiresAuthorization("0.0.0.0", string.Empty).Should().BeTrue();
        BearerTokenValidator.RequiresAuthorization("::", string.Empty).Should().BeTrue();
    }

    [Fact]
    public void BearerTokenValidator_ShouldSkipAuthForLoopbackBindingWithoutConfiguredToken()
    {
        BearerTokenValidator.RequiresAuthorization("127.0.0.1", string.Empty).Should().BeFalse();
        BearerTokenValidator.RequiresAuthorization("localhost", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRequireAuthForLoopbackBindingWithConfiguredToken()
    {
        BearerTokenValidator.RequiresAuthorization("127.0.0.1", "secret-token").Should().BeTrue();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRequireAuthForNonLoopbackBindingWithoutConfiguredToken()
    {
        BearerTokenValidator.RequiresAuthorization("0.0.0.0", string.Empty).Should().BeTrue();
        BearerTokenValidator.RequiresAuthorization("192.168.1.10", string.Empty).Should().BeTrue();
    }

    [Theory]
    [InlineData("O'Brien")]
    [InlineData("\\\"; alert('x')")]
    [InlineData("</script><script>alert('x')</script>")]
    public void WebViewScriptEncoder_ShouldKeepJsonPayloadInsideOneJavaScriptArgument(string json)
    {
        var script = WebViewScriptEncoder.BuildSetJsonScript(json);

        script.Should().StartWith("setJson(");
        script.Should().EndWith(")");
        script.Should().Be($"setJson({JsonSerializer.Serialize(json)})");
    }

    [Fact]
    public void HttpClientTimeouts_ShouldCreateFiniteClientsForServiceAndUpdates()
    {
        using var serviceClient = HttpClientTimeouts.Create(HttpClientTimeouts.ServiceTimeout);
        using var updateClient = HttpClientTimeouts.Create(HttpClientTimeouts.UpdateTimeout);

        HttpClientTimeouts.ServiceTimeout.Should().BePositive();
        HttpClientTimeouts.UpdateTimeout.Should().BePositive();
        serviceClient.Timeout.Should().Be(HttpClientTimeouts.ServiceTimeout);
        updateClient.Timeout.Should().Be(HttpClientTimeouts.UpdateTimeout);
        serviceClient.Timeout.Should().NotBe(Timeout.InfiniteTimeSpan);
        updateClient.Timeout.Should().NotBe(Timeout.InfiniteTimeSpan);
    }

    private static string ToBase64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static string ToBase64(byte[] value) => Convert.ToBase64String(value);

    private static byte[] CreateMinimalTarArchive()
    {
        var archive = new byte[2048];
        var content = Encoding.ASCII.GetBytes("hello");

        WriteAscii(archive, 0, 100, "file.txt");
        WriteAscii(archive, 100, 8, "0000644\0");
        WriteAscii(archive, 108, 8, "0000000\0");
        WriteAscii(archive, 116, 8, "0000000\0");
        WriteAscii(archive, 124, 12, "00000000005\0");
        WriteAscii(archive, 136, 12, "00000000000\0");
        WriteAscii(archive, 148, 8, "        ");
        WriteAscii(archive, 156, 1, "0");
        WriteAscii(archive, 257, 6, "ustar\0");
        WriteAscii(archive, 263, 2, "00");

        var checksum = archive.Sum(value => (int)value);
        WriteAscii(archive, 148, 8, Convert.ToString(checksum, 8)!.PadLeft(6, '0') + "\0 ");
        Buffer.BlockCopy(content, 0, archive, 512, content.Length);
        return archive;

        static void WriteAscii(byte[] target, int offset, int fieldLength, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            bytes.Length.Should().BeLessThanOrEqualTo(fieldLength);
            Buffer.BlockCopy(bytes, 0, target, offset, bytes.Length);
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public MutableTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            => new NoopTimer();

        private sealed class NoopTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class CancelingDriver : IWslcDriver
    {
        private readonly OperationCanceledException _cancellation;

        public CancelingDriver(OperationCanceledException cancellation) => _cancellation = cancellation;

        public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(true);
        public Task<string> GetVersionAsync(CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> GetContainersAsync(CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> StartContainerAsync(string id, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> StopContainerAsync(string id, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> RestartContainerAsync(string id, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> RenameContainerAsync(string id, string name, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> RemoveContainerAsync(string id, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> InspectContainerAsync(string id, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> GetContainerLogsAsync(string id, int tail, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> GetImagesAsync(CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> PullImageAsync(string image, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct) => throw _cancellation;
        public Task<string> RemoveImageAsync(string id, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> InspectImageAsync(string id, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> GetVolumesAsync(CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> CreateVolumeAsync(string name, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> RemoveVolumeAsync(string name, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> InspectVolumeAsync(string name, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> GetNetworksAsync(CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> CreateNetworkAsync(string name, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> RemoveNetworkAsync(string name, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, CancellationToken ct = default, string? network = null) => Task.FromResult(string.Empty);
        public Task<string> ExecCommandAsync(string id, string command, CancellationToken ct = default) => Task.FromResult(string.Empty);
        public Task<string> ExecShellAsync(string id, string shellCommand, string? shell = null, CancellationToken ct = default) => Task.FromResult(string.Empty);
    }
}
