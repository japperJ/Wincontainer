namespace WinContainers.Core.Models;

public static class BearerTokenValidator
{
    public static bool IsAuthorized(string? authorizationHeader, string expectedToken)
    {
        var providedToken = authorizationHeader is null
            ? string.Empty
            : authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authorizationHeader["Bearer ".Length..]
                : authorizationHeader;

        return string.Equals(providedToken, expectedToken, StringComparison.Ordinal);
    }
}
