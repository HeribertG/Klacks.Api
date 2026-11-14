// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parses and canonicalises the redirect targets an administrator added to the trusted list. A target is an
/// https address with a DNS host (no IP literal, no localhost), the default port, no userinfo, query, fragment,
/// escape or backslash, and a path made of plain segments without dot segments or empty segments. The canonical
/// form is lower-case ASCII (punycode) host plus the path exactly as written; the matcher compares it ordinally
/// with the raw redirect URI. The stored setting is a JSON array of strings; any invalid entry makes the whole
/// value invalid when validating, and is skipped (fail closed) when reading.
/// </summary>
/// <param name="input">Target as typed by the administrator</param>
/// <param name="json">Value of the custom targets setting</param>

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.OAuth;

public static class OAuthCustomTargetParser
{
    private const string HttpsPrefix = "https://";

    private const string DefaultPortText = "443";

    private const char PortSeparator = ':';

    private const int MaxDnsLabelLength = 63;

    private const string HostPattern = @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)*\z";

    private const string PathPattern = @"^(/[A-Za-z0-9._~-]+)*/?\z";

    private const string CurrentDirectorySegment = ".";

    private const string ParentDirectorySegment = "..";

    private const string ForbiddenCharacters = "?#\\@%";

    private static readonly Regex HostRegex = new(HostPattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static readonly Regex PathRegex = new(PathPattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static readonly IdnMapping Idn = new();

    public static bool TryCanonicalize(string? input, out string canonical, out string error)
    {
        canonical = string.Empty;
        var trimmed = input?.Trim() ?? string.Empty;

        if (trimmed.Length == 0 || trimmed.Length > OAuthTrustedRedirects.MaxRedirectUriLength)
        {
            error = "The target must not be empty or longer than the allowed length.";
            return false;
        }

        if (!trimmed.StartsWith(HttpsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            error = "The target must start with https://.";
            return false;
        }

        var rest = trimmed[HttpsPrefix.Length..];
        if (rest.IndexOfAny(ForbiddenCharacters.ToCharArray()) >= 0 || rest.Any(char.IsWhiteSpace) || rest.Any(char.IsControl))
        {
            error = "The target must not contain userinfo, query, fragment, escapes, backslashes or whitespace.";
            return false;
        }

        var pathStart = rest.IndexOf('/');
        var authority = pathStart < 0 ? rest : rest[..pathStart];
        var path = pathStart < 0 ? string.Empty : rest[pathStart..];

        if (!TryReadHost(authority, out var host, out error))
        {
            return false;
        }

        if (!PathRegex.IsMatch(path) || HasDotSegment(path))
        {
            error = "The path must consist of plain segments without '.', '..' or empty segments.";
            return false;
        }

        canonical = HttpsPrefix + host + path;
        error = string.Empty;
        return true;
    }

    public static IReadOnlyList<string> ParseCanonical(string? json)
    {
        var entries = ReadEntries(json);
        var result = new List<string>();
        if (entries == null)
        {
            return result;
        }

        foreach (var entry in entries)
        {
            if (TryCanonicalize(entry, out var canonical, out _) && !result.Contains(canonical, StringComparer.Ordinal))
            {
                result.Add(canonical);
            }
        }

        return result;
    }

    public static bool TryValidate(string? json, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        var entries = ReadEntries(json);
        if (entries == null)
        {
            error = "The value must be a JSON array of strings.";
            return false;
        }

        if (entries.Count > OAuthTrustedRedirects.MaxCustomTargets)
        {
            error = $"At most {OAuthTrustedRedirects.MaxCustomTargets} targets are allowed.";
            return false;
        }

        foreach (var entry in entries)
        {
            if (!TryCanonicalize(entry, out _, out var entryError))
            {
                error = $"Target '{entry}' is invalid. {entryError}";
                return false;
            }
        }

        return true;
    }

    private static List<string>? ReadEntries(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryReadHost(string authority, out string host, out string error)
    {
        host = string.Empty;
        var hostText = authority;

        var portIndex = authority.LastIndexOf(PortSeparator);
        if (portIndex >= 0)
        {
            var port = authority[(portIndex + 1)..];
            if (!string.Equals(port, DefaultPortText, StringComparison.Ordinal))
            {
                error = "Only the default https port is allowed.";
                return false;
            }

            hostText = authority[..portIndex];
        }

        if (hostText.Length == 0)
        {
            error = "The host is missing.";
            return false;
        }

        string ascii;
        try
        {
            ascii = Idn.GetAscii(hostText).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            error = "The host is not a valid domain name.";
            return false;
        }

        if (!HostRegex.IsMatch(ascii) || ascii.Split('.').Any(label => label.Length > MaxDnsLabelLength))
        {
            error = "The host is not a valid domain name.";
            return false;
        }

        if (IPAddress.TryParse(ascii, out _) || ascii.All(c => char.IsAsciiDigit(c) || c == '.'))
        {
            error = "IP addresses are not allowed.";
            return false;
        }

        if (string.Equals(ascii, OAuthTrustedRedirects.LoopbackHostLocalhost, StringComparison.Ordinal))
        {
            error = "Use the local programs switch for programs on this computer.";
            return false;
        }

        host = ascii;
        error = string.Empty;
        return true;
    }

    private static bool HasDotSegment(string path)
    {
        return path.Split('/').Any(segment => segment == CurrentDirectorySegment || segment == ParentDirectorySegment);
    }
}
