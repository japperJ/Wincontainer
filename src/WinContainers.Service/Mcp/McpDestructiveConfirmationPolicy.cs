using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace WinContainers.Service.Mcp;

public enum McpDestructiveApprovalStatus
{
    Pending,
    Approved,
    Denied
}

public sealed record DestructiveConfirmationOperation(
    string OperationId,
    DateTimeOffset ExpiresAtUtc,
    string ToolName = "",
    string DisplaySummary = "",
    string SessionId = "unknown",
    string SessionName = "unknown",
    bool SessionVisibleInUi = true,
    bool SessionIsAdmin = false,
    string? SessionWarning = null,
    McpDestructiveApprovalStatus ApprovalStatus = McpDestructiveApprovalStatus.Pending);

public sealed class McpDestructiveApprovalRequest : EventArgs
{
    public McpDestructiveApprovalRequest(
        string operationId,
        string toolName,
        string displaySummary,
        DateTimeOffset expiresAtUtc,
        string sessionId,
        string sessionName,
        bool sessionVisibleInUi,
        bool sessionIsAdmin,
        string? sessionWarning)
    {
        OperationId = operationId;
        ToolName = toolName;
        DisplaySummary = displaySummary;
        ExpiresAtUtc = expiresAtUtc;
        SessionId = sessionId;
        SessionName = sessionName;
        SessionVisibleInUi = sessionVisibleInUi;
        SessionIsAdmin = sessionIsAdmin;
        SessionWarning = sessionWarning;
    }

    public string OperationId { get; }
    public string ToolName { get; }
    public string DisplaySummary { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public string SessionId { get; }
    public string SessionName { get; }
    public bool SessionVisibleInUi { get; }
    public bool SessionIsAdmin { get; }
    public string? SessionWarning { get; }
}

public static class McpDestructiveConfirmationPolicy
{
    private static readonly Dictionary<string, DestructiveConfirmationRecord> Operations = new(StringComparer.Ordinal);
    private static readonly object OperationsSyncRoot = new();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ExpiredRecordRetention = TimeSpan.FromMinutes(3);

    public static bool Enabled { get; private set; } = true;

    public static event EventHandler<McpDestructiveApprovalRequest>? ApprovalRequested;

    public static void SetEnabled(bool enabled) => Enabled = enabled;

    public static string CanonicalizeArguments(params string[] values)
    {
        var normalized = values
            .Select(value => value ?? string.Empty)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrEmpty(value))
            .ToArray();

        var input = string.Join("|", normalized.Select((value, index) => $"{index}:{value}"));
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public static DestructiveConfirmationOperation IssueOperation(
        string toolName,
        string canonicalArguments,
        string? displaySummary = null,
        string? sessionId = null,
        string? sessionName = null,
        bool sessionVisibleInUi = true,
        bool sessionIsAdmin = false,
        string? sessionWarning = null)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        CleanupExpired(nowUtc);

        var approvalSubscribers = ApprovalRequested;
        var operationId = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        var expiresAtUtc = nowUtc.Add(DefaultTtl);
        var record = new DestructiveConfirmationRecord(
            toolName,
            canonicalArguments,
            expiresAtUtc,
            displaySummary ?? $"{toolName} destructive action",
            sessionId ?? "unknown",
            sessionName ?? "unknown",
            sessionVisibleInUi,
            sessionIsAdmin,
            sessionWarning,
            approvalSubscribers is null);

        lock (OperationsSyncRoot)
        {
            Operations[operationId] = record;
        }

        var request = new McpDestructiveApprovalRequest(
            operationId,
            record.ToolName,
            record.DisplaySummary,
            record.ExpiresAtUtc,
            record.SessionId,
            record.SessionName,
            record.SessionVisibleInUi,
            record.SessionIsAdmin,
            record.SessionWarning);

        DispatchApprovalRequest(approvalSubscribers, request, record);

        return new DestructiveConfirmationOperation(
            operationId,
            expiresAtUtc,
            record.ToolName,
            record.DisplaySummary,
            record.SessionId,
            record.SessionName,
            record.SessionVisibleInUi,
            record.SessionIsAdmin,
            record.SessionWarning);
    }

    private static void DispatchApprovalRequest(
        EventHandler<McpDestructiveApprovalRequest>? subscribers,
        McpDestructiveApprovalRequest request,
        DestructiveConfirmationRecord record)
    {
        var subscriberFailed = false;

        if (subscribers is not null)
        {
            foreach (EventHandler<McpDestructiveApprovalRequest> subscriber in subscribers.GetInvocationList())
            {
                try
                {
                    subscriber(null, request);
                }
                catch (Exception ex)
                {
                    subscriberFailed = true;
                    Trace.TraceError(
                        "ApprovalRequested subscriber failed for operation {0}: {1}",
                        request.OperationId,
                        ex);
                }
            }
        }

        lock (record.SyncRoot)
        {
            record.ApprovalRequestDispatching = false;
            if (subscriberFailed)
            {
                record.ApprovalUnavailable = true;
                record.ApprovalStatus = McpDestructiveApprovalStatus.Denied;
            }
        }
    }

    public static bool TryApprove(string operationId, DateTimeOffset? nowUtc = null)
        => TrySetApproval(operationId, McpDestructiveApprovalStatus.Approved, nowUtc);

    public static bool TryApprove(string operationId, out string reason, DateTimeOffset? nowUtc = null)
        => TrySetApproval(operationId, McpDestructiveApprovalStatus.Approved, out reason, nowUtc);

    public static bool TryReject(string operationId, DateTimeOffset? nowUtc = null)
        => TrySetApproval(operationId, McpDestructiveApprovalStatus.Denied, nowUtc);

    public static bool TryReject(string operationId, out string reason, DateTimeOffset? nowUtc = null)
        => TrySetApproval(operationId, McpDestructiveApprovalStatus.Denied, out reason, nowUtc);

    public static bool TryGetApprovalStatus(
        string operationId,
        out McpDestructiveApprovalStatus status,
        out string reason)
    {
        status = McpDestructiveApprovalStatus.Pending;
        reason = string.Empty;

        DestructiveConfirmationRecord? record;
        lock (OperationsSyncRoot)
        {
            Operations.TryGetValue(operationId, out record);
        }

        if (record is null)
        {
            reason = "unknown operation id";
            return false;
        }

        lock (record.SyncRoot)
        {
            status = record.ApprovalStatus;
            if (record.IsExpired)
            {
                reason = "operation expired";
                return false;
            }
            return true;
        }
    }

    public static bool TryConsume(
        string toolName,
        string operationId,
        string canonicalArguments,
        out string reason,
        DateTimeOffset? nowUtc = null)
    {
        reason = string.Empty;

        if (!Enabled)
        {
            return true;
        }

        var utcNow = nowUtc ?? DateTimeOffset.UtcNow;

        DestructiveConfirmationRecord? record;
        lock (OperationsSyncRoot)
        {
            Operations.TryGetValue(operationId, out record);
        }

        if (record is null)
        {
            reason = "unknown operation id";
            return false;
        }

        lock (record.SyncRoot)
        {
            if (record.ExpiresAtUtc <= utcNow)
            {
                record.IsExpired = true;
                reason = "operation expired";
                return false;
            }

            if (!string.Equals(record.ToolName, toolName, StringComparison.Ordinal))
            {
                reason = "tool name does not match issued operation";
                return false;
            }

            if (!string.Equals(record.CanonicalArguments, canonicalArguments, StringComparison.Ordinal))
            {
                reason = "normalized arguments do not match issued operation";
                return false;
            }

            if (record.ApprovalRequestDispatching)
            {
                reason = "human approval is still pending";
                return false;
            }

            if (record.ApprovalUnavailable)
            {
                reason = "human approval UI is unavailable";
                return false;
            }

            if (record.ApprovalStatus == McpDestructiveApprovalStatus.Pending)
            {
                reason = "human approval is still pending";
                return false;
            }

            if (record.ApprovalStatus == McpDestructiveApprovalStatus.Denied)
            {
                reason = "human approval was denied";
                return false;
            }

            if (record.Consumed)
            {
                reason = "operation id already used";
                return false;
            }

            record.Consumed = true;
            return true;
        }
    }

    private static bool TrySetApproval(
        string operationId,
        McpDestructiveApprovalStatus approvalStatus,
        DateTimeOffset? nowUtc)
    {
        return TrySetApproval(operationId, approvalStatus, out _, nowUtc);
    }

    private static bool TrySetApproval(
        string operationId,
        McpDestructiveApprovalStatus approvalStatus,
        out string reason,
        DateTimeOffset? nowUtc)
    {
        reason = string.Empty;
        var utcNow = nowUtc ?? DateTimeOffset.UtcNow;
        DestructiveConfirmationRecord? record;
        lock (OperationsSyncRoot)
        {
            Operations.TryGetValue(operationId, out record);
        }

        if (record is null)
        {
            reason = "unknown operation id";
            return false;
        }

        lock (record.SyncRoot)
        {
            if (record.ExpiresAtUtc <= utcNow || record.IsExpired)
            {
                record.IsExpired = true;
                reason = "operation expired";
                return false;
            }

            if (record.ApprovalUnavailable)
            {
                reason = "human approval UI is unavailable";
                return false;
            }

            if (record.Consumed)
            {
                reason = "operation id already used";
                return false;
            }

            if (record.ApprovalStatus == McpDestructiveApprovalStatus.Denied)
            {
                reason = "human approval was already denied";
                return false;
            }

            if (record.ApprovalStatus == McpDestructiveApprovalStatus.Approved)
            {
                reason = "human approval was already approved";
                return false;
            }

            record.ApprovalStatus = approvalStatus;
            return true;
        }
    }

    private static void CleanupExpired(DateTimeOffset nowUtc)
    {
        KeyValuePair<string, DestructiveConfirmationRecord>[] operations;
        lock (OperationsSyncRoot)
        {
            operations = Operations.ToArray();
        }

        foreach (var pair in operations)
        {
            lock (pair.Value.SyncRoot)
            {
                if (pair.Value.ExpiresAtUtc <= nowUtc)
                {
                    pair.Value.IsExpired = true;

                    if (pair.Value.ExpiresAtUtc.Add(ExpiredRecordRetention) <= nowUtc)
                    {
                        RemoveOperation(pair.Key, pair.Value);
                    }
                }
            }
        }
    }

    private static void RemoveOperation(string operationId, DestructiveConfirmationRecord record)
    {
        lock (OperationsSyncRoot)
        {
            if (Operations.TryGetValue(operationId, out var current) &&
                ReferenceEquals(current, record))
            {
                Operations.Remove(operationId);
            }
        }
    }

    private sealed class DestructiveConfirmationRecord
    {
        public DestructiveConfirmationRecord(
            string toolName,
            string canonicalArguments,
            DateTimeOffset expiresAtUtc,
            string displaySummary,
            string sessionId,
            string sessionName,
            bool sessionVisibleInUi,
            bool sessionIsAdmin,
            string? sessionWarning,
            bool approvalUnavailable)
        {
            ToolName = toolName;
            CanonicalArguments = canonicalArguments;
            ExpiresAtUtc = expiresAtUtc;
            DisplaySummary = displaySummary;
            SessionId = sessionId;
            SessionName = sessionName;
            SessionVisibleInUi = sessionVisibleInUi;
            SessionIsAdmin = sessionIsAdmin;
            SessionWarning = sessionWarning;
            ApprovalUnavailable = approvalUnavailable;
            ApprovalRequestDispatching = !approvalUnavailable;
        }

        public string ToolName { get; }
        public string CanonicalArguments { get; }
        public DateTimeOffset ExpiresAtUtc { get; }
        public string DisplaySummary { get; }
        public string SessionId { get; }
        public string SessionName { get; }
        public bool SessionVisibleInUi { get; }
        public bool SessionIsAdmin { get; }
        public string? SessionWarning { get; }
        public McpDestructiveApprovalStatus ApprovalStatus { get; set; }
        public bool ApprovalUnavailable { get; set; }
        public bool ApprovalRequestDispatching { get; set; }
        public bool IsExpired { get; set; }
        public bool Consumed { get; set; }
        public object SyncRoot { get; } = new();
    }
}
