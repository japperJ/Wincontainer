using System.Security.Cryptography;
using System.Text;

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

public sealed record McpDestructiveApprovalRequest(
    string OperationId,
    string ToolName,
    string DisplaySummary,
    DateTimeOffset ExpiresAtUtc,
    string SessionId,
    string SessionName,
    bool SessionVisibleInUi,
    bool SessionIsAdmin,
    string? SessionWarning);

public static class McpDestructiveConfirmationPolicy
{
    private static readonly Dictionary<string, DestructiveConfirmationRecord> Operations = new(StringComparer.Ordinal);
    private static readonly object OperationsSyncRoot = new();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(3);

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
            sessionWarning);

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

        try
        {
            ApprovalRequested?.Invoke(null, request);
        }
        catch
        {
            // A failing subscriber cannot approve an operation. It remains pending
            // and therefore fails closed until it expires.
        }

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

    public static bool TryApprove(string operationId, DateTimeOffset? nowUtc = null)
        => TrySetApproval(operationId, McpDestructiveApprovalStatus.Approved, nowUtc);

    public static bool TryReject(string operationId, DateTimeOffset? nowUtc = null)
        => TrySetApproval(operationId, McpDestructiveApprovalStatus.Denied, nowUtc);

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
                RemoveOperation(operationId, record);
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
        var utcNow = nowUtc ?? DateTimeOffset.UtcNow;
        DestructiveConfirmationRecord? record;
        lock (OperationsSyncRoot)
        {
            Operations.TryGetValue(operationId, out record);
        }

        if (record is null)
        {
            return false;
        }

        lock (record.SyncRoot)
        {
            if (record.ExpiresAtUtc <= utcNow ||
                record.Consumed ||
                record.ApprovalStatus != McpDestructiveApprovalStatus.Pending)
            {
                RemoveOperation(operationId, record);
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
                    RemoveOperation(pair.Key, pair.Value);
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
            string? sessionWarning)
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
        public bool Consumed { get; set; }
        public object SyncRoot { get; } = new();
    }
}
