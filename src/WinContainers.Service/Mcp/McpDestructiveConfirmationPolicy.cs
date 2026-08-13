using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace WinContainers.Service.Mcp;

public sealed record DestructiveConfirmationOperation(string OperationId, DateTimeOffset ExpiresAtUtc);

public static class McpDestructiveConfirmationPolicy
{
    private static readonly ConcurrentDictionary<string, DestructiveConfirmationRecord> Operations = new(StringComparer.Ordinal);
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(3);

    public static bool Enabled { get; private set; } = true;

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

    public static DestructiveConfirmationOperation IssueOperation(string toolName, string canonicalArguments)
    {
        CleanupExpired(DateTimeOffset.UtcNow);

        var operationId = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(DefaultTtl);
        var record = new DestructiveConfirmationRecord(toolName, canonicalArguments, expiresAtUtc);
        Operations[operationId] = record;
        return new DestructiveConfirmationOperation(operationId, expiresAtUtc);
    }

    public static bool TryConsume(string toolName, string operationId, string canonicalArguments, out string reason, DateTimeOffset? nowUtc = null)
    {
        reason = string.Empty;

        if (!Enabled)
        {
            return true;
        }

        var utcNow = nowUtc ?? DateTimeOffset.UtcNow;
        CleanupExpired(utcNow);

        if (!Operations.TryGetValue(operationId, out var record))
        {
            reason = "unknown operation id";
            return false;
        }

        lock (record.SyncRoot)
        {
            if (record.ExpiresAtUtc <= utcNow)
            {
                Operations.TryRemove(operationId, out _);
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

            if (record.Consumed)
            {
                reason = "operation id already used";
                return false;
            }

            record.Consumed = true;
            return true;
        }
    }

    private static void CleanupExpired(DateTimeOffset nowUtc)
    {
        foreach (var pair in Operations)
        {
            if (pair.Value.ExpiresAtUtc <= nowUtc)
            {
                Operations.TryRemove(pair.Key, out _);
            }
        }
    }

    private sealed class DestructiveConfirmationRecord
    {
        public DestructiveConfirmationRecord(string toolName, string canonicalArguments, DateTimeOffset expiresAtUtc)
        {
            ToolName = toolName;
            CanonicalArguments = canonicalArguments;
            ExpiresAtUtc = expiresAtUtc;
        }

        public string ToolName { get; }

        public string CanonicalArguments { get; }

        public DateTimeOffset ExpiresAtUtc { get; }

        public bool Consumed { get; set; }

        public object SyncRoot { get; } = new();
    }
}
