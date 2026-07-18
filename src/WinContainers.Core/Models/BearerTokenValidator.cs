using System.Net;

namespace WinContainers.Core.Models;

public static class BearerTokenValidator
{
    public static bool IsAuthorized(string? authorizationHeader, string expectedToken)
    {
        var providedToken = ExtractToken(authorizationHeader);
        return string.Equals(providedToken, expectedToken, StringComparison.Ordinal);
    }

    public static bool RequiresAuthorization(string? listenHost, string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(expectedToken) && IsLoopbackListenHost(listenHost))
        {
            return false;
        }

        return true;
    }

    public static bool IsLoopbackListenHost(string? listenHost)
    {
        if (string.IsNullOrWhiteSpace(listenHost))
        {
            return true;
        }

        if (string.Equals(listenHost, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(listenHost, "loopback", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(listenHost, out var address))
        {
            return IPAddress.IsLoopback(address);
        }

        return false;
    }

    private static string ExtractToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return string.Empty;
        }

        var header = authorizationHeader.Trim();
        if (header.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return header["Bearer".Length..].TrimStart();
        }

        return header;
    }
}
