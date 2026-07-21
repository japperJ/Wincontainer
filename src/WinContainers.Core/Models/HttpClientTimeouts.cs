namespace WinContainers.Core.Models;

public static class HttpClientTimeouts
{
    // The service timeout matches the runtime's five-minute-plus image operations.
    public static readonly TimeSpan ServiceTimeout = TimeSpan.FromMinutes(30);

    // Update downloads can be large, but should still fail within a bounded period.
    public static readonly TimeSpan UpdateTimeout = TimeSpan.FromMinutes(10);

    public static HttpClient Create(TimeSpan timeout) => new() { Timeout = timeout };
}
