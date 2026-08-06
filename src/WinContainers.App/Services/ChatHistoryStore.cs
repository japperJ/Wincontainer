using System.Text.Json;

namespace WinContainers_App.Services;

/// <summary>One persisted chat turn.</summary>
public sealed record ChatRecord(string Role, string Text);

/// <summary>
/// Persists the AI conversation as JSON under
/// %LOCALAPPDATA%\WinContainers\chats so it survives restarts.
/// </summary>
public sealed class ChatHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _path;
    private readonly object _lock = new();

    public ChatHistoryStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appDataPath, "WinContainers", "chats");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "default.json");
    }

    public List<ChatRecord> Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return [];
                }

                return JsonSerializer.Deserialize<List<ChatRecord>>(File.ReadAllText(_path), JsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatHistoryStore] Load failed: {ex}");
                return [];
            }
        }
    }

    public void Save(IEnumerable<ChatRecord> records)
    {
        lock (_lock)
        {
            try
            {
                File.WriteAllText(_path, JsonSerializer.Serialize(records.ToList(), JsonOptions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatHistoryStore] Save failed: {ex}");
            }
        }
    }
}
