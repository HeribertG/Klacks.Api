// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The four administrator settings of the OAuth server and the redirect trust check built on them. Absent or empty
/// values mean the default (mode trusted, local programs allowed, write allowed, no custom targets); a present but
/// unparseable value fails closed (mode trusted, local programs off, write off, no custom targets). The mode never
/// becomes "open for everyone".
/// </summary>
/// <param name="Mode">Trusted or Off</param>
/// <param name="AllowLoopback">Whether programs on the same computer are accepted</param>
/// <param name="AllowWrite">Whether a connection may be granted the right to change data</param>
/// <param name="CustomTargets">Canonical custom targets added by an administrator</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.OAuth;

namespace Klacks.Api.Domain.Models.Authentification;

public sealed record OAuthTrustSettings(
    OAuthConnectionMode Mode,
    bool AllowLoopback,
    bool AllowWrite,
    IReadOnlyList<string> CustomTargets)
{
    public static IReadOnlyList<string> Keys { get; } =
    [
        SettingKeys.OAuthConnectionsMode,
        SettingKeys.OAuthAllowLoopback,
        SettingKeys.OAuthAllowWrite,
        SettingKeys.OAuthCustomRedirectTargets
    ];

    public static OAuthTrustSettings FromValues(IReadOnlyDictionary<string, string> values)
    {
        return new OAuthTrustSettings(
            ReadMode(Read(values, SettingKeys.OAuthConnectionsMode)),
            ReadSwitch(Read(values, SettingKeys.OAuthAllowLoopback)),
            ReadSwitch(Read(values, SettingKeys.OAuthAllowWrite)),
            OAuthCustomTargetParser.ParseCanonical(Read(values, SettingKeys.OAuthCustomRedirectTargets)));
    }

    public OAuthTrustDecision Evaluate(string? redirectUri)
    {
        var target = OAuthRedirectMatcher.DescribeTarget(redirectUri);

        if (Mode == OAuthConnectionMode.Off)
        {
            return new OAuthTrustDecision(OAuthTrustOutcome.ConnectionsDisabled, null, target);
        }

        var match = OAuthRedirectMatcher.Match(redirectUri, AllowLoopback, CustomTargets);
        if (match != null)
        {
            return new OAuthTrustDecision(OAuthTrustOutcome.Trusted, match, string.Empty);
        }

        if (!AllowLoopback && OAuthRedirectMatcher.IsLoopbackClass(redirectUri))
        {
            return new OAuthTrustDecision(OAuthTrustOutcome.LocalProgramsDisabled, null, target);
        }

        return new OAuthTrustDecision(OAuthTrustOutcome.NotTrusted, null, target);
    }

    private static string? Read(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : null;
    }

    private static OAuthConnectionMode ReadMode(string? value)
    {
        return string.Equals(value?.Trim(), OAuthConnectionModes.Off, StringComparison.OrdinalIgnoreCase)
            ? OAuthConnectionMode.Off
            : OAuthConnectionMode.Trusted;
    }

    private static bool ReadSwitch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return bool.TryParse(value.Trim(), out var parsed) && parsed;
    }
}
