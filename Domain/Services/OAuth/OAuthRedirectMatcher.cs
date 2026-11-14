// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stateless matcher deciding whether a raw OAuth redirect URI belongs to the shipped trusted list, to the
/// loopback class (programs on the same computer) or to the canonical custom targets an administrator added.
/// The raw string is judged character by character and never normalised through Uri, so dot segments, escapes,
/// backslashes, userinfo, query and fragment tricks cannot turn a foreign address into a listed one. Fixed
/// entries and custom targets must be ordinally equal; the ChatGPT callback accepts exactly one path segment
/// after its prefix; loopback accepts http on localhost, 127.0.0.1 and [::1] with any port and a plain path.
/// </summary>
/// <param name="redirectUri">Raw redirect URI exactly as the client sent it</param>
/// <param name="allowLoopback">Whether the loopback class (including the vscode.dev redirect) is accepted</param>
/// <param name="customTargets">Canonical custom targets as returned by OAuthCustomTargetParser</param>

using System.Globalization;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Services.OAuth;

public static class OAuthRedirectMatcher
{
    private const char FirstPrintableAscii = '!';

    private const char LastPrintableAscii = '~';

    private const string SchemeSeparator = "://";

    private const string InvalidTarget = "(invalid address)";

    private const int MaxDescribedTargetLength = 100;


    private const int MaxPortDigits = 5;

    private const RegexOptions PatternOptions = RegexOptions.CultureInvariant | RegexOptions.NonBacktracking;

    private static readonly string ChatGptCallbackIdPattern = "[A-Za-z0-9_-]{1," + OAuthTrustedRedirects.ChatGptCallbackIdMaxLength + "}";

    private static readonly Regex ChatGptRegex = new(
        "^" + Regex.Escape(OAuthTrustedRedirects.ChatGptCallbackPrefix) + ChatGptCallbackIdPattern + @"\z",
        PatternOptions);

    private static readonly string LoopbackPathAndQueryPattern =
        "(?::(?<port>[0-9]{1," + MaxPortDigits + @"}))?(?:/[A-Za-z0-9._~/-]*)?(?:\?[\x21\x22\x24-\x5B\x5D-\x7E]*)?\z";

    private static readonly Regex LoopbackRegex = new(
        "^" + Regex.Escape(OAuthTrustedRedirects.LoopbackScheme)
        + "(?<host>" + string.Join("|", OAuthTrustedRedirects.LoopbackHosts.Select(Regex.Escape)) + ")"
        + LoopbackPathAndQueryPattern,
        PatternOptions);

    public static OAuthRedirectMatch? Match(string? redirectUri, bool allowLoopback, IReadOnlyCollection<string> customTargets)
    {
        if (!IsPlainPrintableAscii(redirectUri))
        {
            return null;
        }

        var raw = redirectUri!;

        foreach (var entry in OAuthTrustedRedirects.VendorEntries)
        {
            if (string.Equals(raw, entry.RedirectUri, StringComparison.Ordinal))
            {
                return new OAuthRedirectMatch(entry.DisplayName, entry.Host, OAuthRedirectClass.Vendor, null);
            }
        }

        if (ChatGptRegex.IsMatch(raw))
        {
            return new OAuthRedirectMatch(
                OAuthTrustedRedirects.ChatGptDisplayName,
                OAuthTrustedRedirects.ChatGptHost,
                OAuthRedirectClass.Vendor,
                null);
        }

        if (allowLoopback)
        {
            var loopback = MatchLoopback(raw);
            if (loopback != null)
            {
                return loopback;
            }
        }

        if (customTargets.Contains(raw, StringComparer.Ordinal))
        {
            return new OAuthRedirectMatch(HostOfCanonicalTarget(raw), HostOfCanonicalTarget(raw), OAuthRedirectClass.Custom, null);
        }

        return null;
    }

    public static bool IsLoopbackClass(string? redirectUri)
    {
        return IsPlainPrintableAscii(redirectUri) && MatchLoopback(redirectUri!) != null;
    }

    public static string DescribeTarget(string? redirectUri)
    {
        if (string.IsNullOrEmpty(redirectUri))
        {
            return InvalidTarget;
        }

        var schemeEnd = redirectUri.IndexOf(SchemeSeparator, StringComparison.Ordinal);
        if (schemeEnd <= 0 || !IsPlainPrintableAscii(redirectUri))
        {
            return InvalidTarget;
        }

        var authorityStart = schemeEnd + SchemeSeparator.Length;
        var authorityEnd = redirectUri.IndexOfAny(['/', '?', '#', '\\'], authorityStart);
        var end = authorityEnd < 0 ? redirectUri.Length : authorityEnd;
        var described = redirectUri[..end];

        return described.Length > MaxDescribedTargetLength ? described[..MaxDescribedTargetLength] : described;
    }

    private static OAuthRedirectMatch? MatchLoopback(string raw)
    {
        if (string.Equals(raw, OAuthTrustedRedirects.VsCodeWebRedirectUri, StringComparison.Ordinal))
        {
            return new OAuthRedirectMatch(
                OAuthTrustedRedirects.LocalProgramDisplayName,
                OAuthTrustedRedirects.VsCodeWebHost,
                OAuthRedirectClass.Loopback,
                null);
        }

        var match = LoopbackRegex.Match(raw);
        if (!match.Success)
        {
            return null;
        }

        int? port = null;
        var portGroup = match.Groups["port"];
        if (portGroup.Success)
        {
            if (!int.TryParse(portGroup.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                || parsed < 1
                || parsed > OAuthTrustedRedirects.MaxPort)
            {
                return null;
            }

            port = parsed;
        }

        return new OAuthRedirectMatch(
            OAuthTrustedRedirects.LocalProgramDisplayName,
            match.Groups["host"].Value,
            OAuthRedirectClass.Loopback,
            port);
    }

    private static bool IsPlainPrintableAscii(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > OAuthTrustedRedirects.MaxRedirectUriLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character < FirstPrintableAscii || character > LastPrintableAscii)
            {
                return false;
            }
        }

        return true;
    }

    private static string HostOfCanonicalTarget(string canonical)
    {
        var start = canonical.IndexOf(SchemeSeparator, StringComparison.Ordinal) + SchemeSeparator.Length;
        var end = canonical.IndexOf('/', start);
        return end < 0 ? canonical[start..] : canonical[start..end];
    }
}
