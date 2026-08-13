using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using WinContainers.Service.Mcp;

namespace WinContainers_App.Services;

public sealed class McpDestructiveApprovalCoordinator : IDisposable
{
    private readonly IDialogService _dialogs;
    private readonly ILogger<McpDestructiveApprovalCoordinator> _logger;
    private readonly object _syncRoot = new();
    private DispatcherQueue? _dispatcherQueue;
    private bool _subscribed;

    public McpDestructiveApprovalCoordinator(
        IDialogService dialogs,
        ILogger<McpDestructiveApprovalCoordinator> logger)
    {
        _dialogs = dialogs;
        _logger = logger;
    }

    public void Subscribe(DispatcherQueue dispatcherQueue)
    {
        lock (_syncRoot)
        {
            if (_subscribed)
            {
                return;
            }

            _dispatcherQueue = dispatcherQueue;
            McpDestructiveConfirmationPolicy.ApprovalRequested += OnApprovalRequested;
            _subscribed = true;
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (!_subscribed)
            {
                return;
            }

            McpDestructiveConfirmationPolicy.ApprovalRequested -= OnApprovalRequested;
            _subscribed = false;
            _dispatcherQueue = null;
        }
    }

    private void OnApprovalRequested(object? sender, McpDestructiveApprovalRequest request)
    {
        DispatcherQueue? dispatcher;
        lock (_syncRoot)
        {
            dispatcher = _dispatcherQueue;
        }

        if (dispatcher is null || !dispatcher.TryEnqueue(() => _ = ShowApprovalAsync(request)))
        {
            McpDestructiveConfirmationPolicy.TryReject(request.OperationId);
            _logger.LogWarning(
                "Denied MCP destructive operation {OperationId}: approval UI is unavailable.",
                request.OperationId);
        }
    }

    private async Task ShowApprovalAsync(McpDestructiveApprovalRequest request)
    {
        try
        {
            var sessionVisibility = request.SessionVisibleInUi ? "visible" : "hidden";
            var sessionRole = request.SessionIsAdmin ? "administrator" : "non-administrator";
            var content =
                $"{request.DisplaySummary}\n\n" +
                $"Session: {request.SessionName} ({sessionVisibility}, {sessionRole})\n" +
                $"Session ID: {request.SessionId}\n" +
                $"Expires: {request.ExpiresAtUtc:O}";

            if (!string.IsNullOrWhiteSpace(request.SessionWarning))
            {
                content += $"\n\n{request.SessionWarning}";
            }

            var result = await _dialogs.ShowConfirmAsync(
                "Confirm destructive MCP action",
                content,
                "Allow",
                "Deny");

            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                if (!McpDestructiveConfirmationPolicy.TryApprove(request.OperationId))
                {
                    _logger.LogWarning(
                        "MCP approval arrived too late for operation {OperationId}.",
                        request.OperationId);
                }
            }
            else
            {
                McpDestructiveConfirmationPolicy.TryReject(request.OperationId);
                _logger.LogInformation(
                    "Denied MCP destructive operation {OperationId} from dialog result {Result}.",
                    request.OperationId,
                    result);
            }
        }
        catch (Exception ex)
        {
            McpDestructiveConfirmationPolicy.TryReject(request.OperationId);
            _logger.LogError(
                ex,
                "Denied MCP destructive operation {OperationId}: approval dialog failed.",
                request.OperationId);
        }
    }
}
