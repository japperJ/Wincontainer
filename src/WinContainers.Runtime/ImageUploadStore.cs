using System.Diagnostics;

namespace WinContainers.Runtime;

public sealed class ImageUploadStore
{
    public const int MaxChunkBytes = 3 * 1024;
    public const long MaxUploadBytes = 512L * 1024 * 1024;

    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);
    private const string MissingUploadMessage = "Validation error: upload ID was not found.";
    private const string ExpiredUploadMessage = "Validation error: upload has expired.";
    private const string InvalidChunkMessage = "Validation error: chunk is not valid base64.";
    private const string OversizedChunkMessage = "Validation error: chunk exceeds 3 KB after decoding.";
    private const string OversizedUploadMessage = "Validation error: upload exceeds 512 MB after decoding.";

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private readonly Dictionary<string, UploadState> _uploads = new(StringComparer.Ordinal);
    private readonly HashSet<string> _expiredUploadIds = new(StringComparer.Ordinal);

    public ImageUploadStore(TimeProvider? timeProvider = null)
        => _timeProvider = timeProvider ?? TimeProvider.System;

    public ImageUploadInfo Start()
    {
        CleanupExpiredUploads();

        while (true)
        {
            var uploadId = Guid.NewGuid().ToString("N");
            var path = GetUploadPath(uploadId);

            lock (_gate)
            {
                if (_uploads.ContainsKey(uploadId) || _expiredUploadIds.Contains(uploadId))
                {
                    continue;
                }

                try
                {
                    CreateExclusiveEmptyFile(path);
                }
                catch (IOException)
                {
                    continue;
                }

                _uploads.Add(uploadId, new UploadState(path, _timeProvider.GetUtcNow()));
                return new ImageUploadInfo(uploadId, MaxChunkBytes, MaxUploadBytes);
            }
        }
    }

    public async Task<string> AppendChunkAsync(string uploadId, int sequence, string base64Chunk, CancellationToken ct)
    {
        CleanupExpiredUploads();

        if (!TryGetActiveUpload(uploadId, out var state, out var currentStateMessage))
        {
            return currentStateMessage;
        }

        await state!.Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!TryConfirmActiveUpload(uploadId, state, out var activeMessage))
            {
                return activeMessage;
            }

            if (sequence != state.NextSequence)
            {
                return $"Validation error: expected chunk sequence {state.NextSequence}.";
            }

            if (string.IsNullOrWhiteSpace(base64Chunk))
            {
                return InvalidChunkMessage;
            }

            byte[] decodedBytes;
            try
            {
                decodedBytes = Convert.FromBase64String(base64Chunk);
            }
            catch (FormatException)
            {
                return InvalidChunkMessage;
            }

            if (decodedBytes.Length > MaxChunkBytes)
            {
                return OversizedChunkMessage;
            }

            if (state.BytesWritten + decodedBytes.LongLength > MaxUploadBytes)
            {
                return OversizedUploadMessage;
            }

            await using (var stream = new FileStream(
                state.FilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.None,
                4096,
                useAsync: true))
            {
                await stream.WriteAsync(decodedBytes, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
            }

            state.BytesWritten += decodedBytes.LongLength;
            state.NextSequence++;
            state.LastActivity = _timeProvider.GetUtcNow();
            return "Upload chunk accepted.";
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public async Task<string> CompleteAsync(
        string uploadId,
        Func<string, CancellationToken, Task<string>> loadAsync,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(loadAsync);

        CleanupExpiredUploads();

        if (!TryGetActiveUpload(uploadId, out var state, out var currentStateMessage))
        {
            return currentStateMessage;
        }

        await state!.Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!TryConfirmActiveUpload(uploadId, state, out var activeMessage))
            {
                return activeMessage;
            }

            var now = _timeProvider.GetUtcNow();
            if (IsExpired(state, now))
            {
                MarkExpiredLocked(uploadId, state);
                TryDeleteFile(state.FilePath);
                return ExpiredUploadMessage;
            }

            lock (_gate)
            {
                _uploads.Remove(uploadId);
            }

            try
            {
                return await loadAsync(state.FilePath, ct).ConfigureAwait(false);
            }
            finally
            {
                TryDeleteFile(state.FilePath);
            }
        }
        finally
        {
            state.Gate.Release();
        }
    }

    private bool TryGetActiveUpload(string uploadId, out UploadState? state, out string message)
    {
        state = null;

        if (string.IsNullOrWhiteSpace(uploadId))
        {
            message = MissingUploadMessage;
            return false;
        }

        lock (_gate)
        {
            if (_uploads.TryGetValue(uploadId, out state))
            {
                message = string.Empty;
                return true;
            }

            message = _expiredUploadIds.Contains(uploadId)
                ? ExpiredUploadMessage
                : MissingUploadMessage;
            return false;
        }
    }

    private bool TryConfirmActiveUpload(string uploadId, UploadState state, out string message)
    {
        lock (_gate)
        {
            if (_uploads.TryGetValue(uploadId, out var current) && ReferenceEquals(current, state))
            {
                message = string.Empty;
                return true;
            }
        }

        message = state.IsExpired ? ExpiredUploadMessage : MissingUploadMessage;
        return false;
    }

    private void CleanupExpiredUploads()
    {
        var now = _timeProvider.GetUtcNow();
        var expiredUploads = new List<(string UploadId, UploadState State)>();

        lock (_gate)
        {
            foreach (var upload in _uploads.ToArray())
            {
                if (!IsExpired(upload.Value, now))
                {
                    continue;
                }

                if (!upload.Value.Gate.Wait(0))
                {
                    continue;
                }

                MarkExpiredLocked(upload.Key, upload.Value);
                expiredUploads.Add((upload.Key, upload.Value));
            }
        }

        foreach (var expired in expiredUploads)
        {
            TryDeleteFile(expired.State.FilePath);
            expired.State.Gate.Release();
        }
    }

    private static bool IsExpired(UploadState state, DateTimeOffset now) =>
        now - state.LastActivity >= UploadLifetime;

    private void MarkExpiredLocked(string uploadId, UploadState state)
    {
        state.IsExpired = true;
        _uploads.Remove(uploadId);
        _expiredUploadIds.Add(uploadId);
    }

    private static string GetUploadPath(string uploadId) =>
        Path.Combine(Path.GetTempPath(), $"{uploadId}.tar");

    private static void CreateExclusiveEmptyFile(string path)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[ImageUploadStore] Temp file cleanup failed: {ex}");
        }
    }

    private sealed class UploadState
    {
        public UploadState(string filePath, DateTimeOffset lastActivity)
        {
            FilePath = filePath;
            LastActivity = lastActivity;
        }

        public string FilePath { get; }
        public int NextSequence { get; set; }
        public long BytesWritten { get; set; }
        public DateTimeOffset LastActivity { get; set; }
        public bool IsExpired { get; set; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}

public sealed record ImageUploadInfo(string UploadId, int MaxChunkBytes, long MaxUploadBytes);
