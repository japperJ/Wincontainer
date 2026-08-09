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
    private const int MaxRecentlyExpiredUploadIds = 1024;

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private readonly Dictionary<string, UploadState> _uploads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _recentlyExpiredUploadIds = new(StringComparer.Ordinal);
    private readonly Queue<(string UploadId, DateTimeOffset ExpiredAt)> _recentlyExpiredUploadOrder = new();

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
                if (_uploads.ContainsKey(uploadId))
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

        state!.Lease.RetainOperation();
        var deleteFile = false;
        var gateHeld = false;
        await state!.Lease.Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            gateHeld = true;

            if (!TryConfirmActiveUpload(uploadId, state, out var activeMessage))
            {
                deleteFile = state.Lease.IsExpired;
                return activeMessage;
            }

            var now = _timeProvider.GetUtcNow();
            if (IsExpired(state, now))
            {
                lock (_gate)
                {
                    TryRemoveExpiredUploadLocked(uploadId, state, now);
                }

                deleteFile = true;
                return ExpiredUploadMessage;
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
            if (gateHeld)
            {
                state.Lease.Gate.Release();
            }

            if (deleteFile)
            {
                TryDeleteFile(state.FilePath);
            }

            state.Lease.ReleaseOperation();
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

        state!.Lease.RetainOperation();
        var deleteFile = false;
        var gateHeld = false;
        await state!.Lease.Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            gateHeld = true;

            if (!TryConfirmActiveUpload(uploadId, state, out var activeMessage))
            {
                deleteFile = state.Lease.IsExpired;
                return activeMessage;
            }

            var now = _timeProvider.GetUtcNow();
            if (IsExpired(state, now))
            {
                lock (_gate)
                {
                    TryRemoveExpiredUploadLocked(uploadId, state, now);
                }

                deleteFile = true;
                return ExpiredUploadMessage;
            }

            lock (_gate)
            {
                if (_uploads.TryGetValue(uploadId, out var current) && ReferenceEquals(current, state))
                {
                    _uploads.Remove(uploadId);
                    state.Lease.MarkRemoved(expired: false);
                }
            }

            try
            {
                gateHeld = false;
                state.Lease.Gate.Release();
                return await loadAsync(state.FilePath, ct).ConfigureAwait(false);
            }
            finally
            {
                deleteFile = true;
            }
        }
        finally
        {
            if (gateHeld)
            {
                state.Lease.Gate.Release();
            }

            if (deleteFile)
            {
                TryDeleteFile(state.FilePath);
            }

            state.Lease.ReleaseOperation();
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

            if (_recentlyExpiredUploadIds.ContainsKey(uploadId))
            {
                message = ExpiredUploadMessage;
                return false;
            }

            message = MissingUploadMessage;
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

        message = state.Lease.IsExpired ? ExpiredUploadMessage : MissingUploadMessage;
        return false;
    }

    private void CleanupExpiredUploads()
    {
        var now = _timeProvider.GetUtcNow();
        var expiredUploads = new List<(string UploadId, UploadState State)>();

        lock (_gate)
        {
            PruneRecentlyExpiredUploadsLocked(now);

            foreach (var upload in _uploads.ToArray())
            {
                if (!IsExpired(upload.Value, now))
                {
                    continue;
                }

                if (!upload.Value.Lease.Gate.Wait(0))
                {
                    continue;
                }

                upload.Value.Lease.RetainOperation();

                if (TryRemoveExpiredUploadLocked(upload.Key, upload.Value, now))
                {
                    expiredUploads.Add((upload.Key, upload.Value));
                }

                upload.Value.Lease.Gate.Release();
            }
        }

        foreach (var expired in expiredUploads)
        {
            TryDeleteFile(expired.State.FilePath);
            expired.State.Lease.ReleaseOperation();
        }
    }

    private void RecordRecentlyExpiredUploadLocked(string uploadId, DateTimeOffset expiredAt)
    {
        _recentlyExpiredUploadIds[uploadId] = expiredAt;
        _recentlyExpiredUploadOrder.Enqueue((uploadId, expiredAt));
        PruneRecentlyExpiredUploadsLocked(expiredAt);
    }

    private void PruneRecentlyExpiredUploadsLocked(DateTimeOffset now)
    {
        while (_recentlyExpiredUploadOrder.Count > 0)
        {
            var (uploadId, expiredAt) = _recentlyExpiredUploadOrder.Peek();
            if (now - expiredAt < UploadLifetime && _recentlyExpiredUploadOrder.Count <= MaxRecentlyExpiredUploadIds)
            {
                break;
            }

            _recentlyExpiredUploadOrder.Dequeue();

            if (_recentlyExpiredUploadIds.TryGetValue(uploadId, out var recordedAt) && recordedAt == expiredAt)
            {
                _recentlyExpiredUploadIds.Remove(uploadId);
            }
        }
    }

    private bool TryRemoveExpiredUploadLocked(string uploadId, UploadState state, DateTimeOffset expiredAt)
    {
        if (_uploads.TryGetValue(uploadId, out var current) && ReferenceEquals(current, state))
        {
            _uploads.Remove(uploadId);
            state.Lease.MarkRemoved(expired: true);
            RecordRecentlyExpiredUploadLocked(uploadId, expiredAt);
            return true;
        }

        return false;
    }

    private static bool IsExpired(UploadState state, DateTimeOffset now) =>
        now - state.LastActivity >= UploadLifetime;

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
        public UploadLease Lease { get; } = new();
    }

    private sealed class UploadLease
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        private int _retainedOperations;
        private int _removed;
        private int _expired;
        private int _gateDisposed;

        public bool IsExpired => Volatile.Read(ref _expired) == 1;

        public void RetainOperation() => Interlocked.Increment(ref _retainedOperations);

        public void MarkExpired() => Volatile.Write(ref _expired, 1);

        public void ReleaseOperation()
        {
            if (Interlocked.Decrement(ref _retainedOperations) == 0 && Volatile.Read(ref _removed) == 1)
            {
                DisposeGate();
            }
        }

        public void MarkRemoved(bool expired)
        {
            if (expired)
            {
                MarkExpired();
            }

            Volatile.Write(ref _removed, 1);

            if (Volatile.Read(ref _retainedOperations) == 0)
            {
                DisposeGate();
            }
        }

        private void DisposeGate()
        {
            if (Interlocked.Exchange(ref _gateDisposed, 1) == 0)
            {
                Gate.Dispose();
            }
        }
    }
}

public sealed record ImageUploadInfo(string UploadId, int MaxChunkBytes, long MaxUploadBytes);
