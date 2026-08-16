using System.Globalization;
using System.Text.RegularExpressions;

namespace WinContainers.Runtime;

public sealed record PortBindingConversionResult(
    bool Success,
    IReadOnlyList<string> Bindings,
    string? Error);

public static class PortBindingConverter
{
    private static readonly Regex PortPattern = new(
        @"^(?<host>\d+)(?::(?<container>\d+))(?<protocol>/[A-Za-z][A-Za-z0-9+.-]*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static PortBindingConversionResult Convert(string? bindings, bool allowLocalNetworkAccess)
    {
        if (string.IsNullOrWhiteSpace(bindings))
            return Failure("No published port bindings were provided.");

        return Convert(
            bindings.Split(',', StringSplitOptions.TrimEntries),
            allowLocalNetworkAccess);
    }

    public static PortBindingConversionResult Convert(
        IEnumerable<string>? bindings,
        bool allowLocalNetworkAccess)
    {
        if (bindings is null)
            return Failure("No published port bindings were provided.");

        var converted = new List<string>();
        foreach (var rawBinding in bindings.SelectMany(binding =>
                     (binding ?? string.Empty).Split(',', StringSplitOptions.TrimEntries)))
        {
            if (!TryParse(rawBinding, out var hostPort, out var containerPort, out var protocol, out var error))
                return Failure(error!);

            var hostAddress = allowLocalNetworkAccess ? "0.0.0.0" : "127.0.0.1";
            var suffix = string.IsNullOrWhiteSpace(protocol) ? string.Empty : $"/{protocol}";
            converted.Add($"{hostAddress}:{hostPort}:{containerPort}{suffix}");
        }

        return converted.Count == 0
            ? Failure("No published port bindings were provided.")
            : new PortBindingConversionResult(true, converted, null);
    }

    public static bool TryParse(
        string? binding,
        out int hostPort,
        out int containerPort,
        out string? protocol,
        out string? error)
    {
        hostPort = 0;
        containerPort = 0;
        protocol = null;
        error = null;

        if (string.IsNullOrWhiteSpace(binding))
        {
            error = "Published port binding cannot be empty.";
            return false;
        }

        var value = binding.Trim();
        var arrowIndex = value.IndexOf("->", StringComparison.Ordinal);
        string hostPart;
        string containerPart;
        if (arrowIndex >= 0)
        {
            if (value.IndexOf("->", arrowIndex + 2, StringComparison.Ordinal) >= 0)
            {
                error = $"Malformed published port binding '{binding}'.";
                return false;
            }

            hostPart = value[..arrowIndex].Trim();
            containerPart = value[(arrowIndex + 2)..].Trim();
        }
        else
        {
            var parts = value.Split(':', StringSplitOptions.None);
            if (parts.Length == 2)
            {
                hostPart = parts[0].Trim();
                containerPart = parts[1].Trim();
            }
            else if (parts.Length == 3)
            {
                if (!IsSupportedHostAddress(parts[0].Trim()))
                {
                    error = $"Unsupported host bind address '{parts[0].Trim()}'.";
                    return false;
                }

                hostPart = parts[1].Trim();
                containerPart = parts[2].Trim();
            }
            else
            {
                error = $"Malformed published port binding '{binding}'.";
                return false;
            }
        }

        if (hostPart.Contains(':', StringComparison.Ordinal))
        {
            var addressSeparator = hostPart.LastIndexOf(':');
            var address = hostPart[..addressSeparator].Trim();
            hostPart = hostPart[(addressSeparator + 1)..].Trim();
            if (!IsSupportedHostAddress(address))
            {
                error = $"Unsupported host bind address '{address}'.";
                return false;
            }
        }

        var match = PortPattern.Match($"{hostPart}:{containerPart}");
        if (!match.Success)
        {
            error = $"Malformed published port binding '{binding}'.";
            return false;
        }

        if (!int.TryParse(match.Groups["host"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out hostPort)
            || !int.TryParse(match.Groups["container"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out containerPort)
            || hostPort is < 1 or > 65535
            || containerPort is < 1 or > 65535)
        {
            error = $"Published ports in '{binding}' must be between 1 and 65535.";
            return false;
        }

        var protocolGroup = match.Groups["protocol"].Value;
        protocol = protocolGroup.Length > 1 ? protocolGroup[1..] : null;
        return true;
    }

    private static bool IsSupportedHostAddress(string address) =>
        address == "127.0.0.1"
        || address == "0.0.0.0";

    private static PortBindingConversionResult Failure(string error) =>
        new(false, Array.Empty<string>(), error);
}
