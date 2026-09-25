// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns an OUTPUT channel scan into an actionable refusal for the assistant's macro skills: only the
/// channels the backend processes (<see cref="MacroOutputChannels.Supported"/>) are allowed, every channel
/// must be a plain number, and a scan that could not complete is a refusal, never a pass.
/// </summary>
/// <param name="scan">The channel scan of the script or appended script block the skill is about to store</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Application.Skills;

public static class MacroOutputChannelPolicy
{
    private const string ScanFailedMessage =
        "The OUTPUT statements of the script could not be analysed ({0}). Fix the script and try again.";

    private const string ComputedChannelMessage =
        "Every OUTPUT statement must name its channel as a plain number, for example OUTPUT 10, value; a computed "
        + "channel cannot be checked. {0}";

    private const string UnsupportedChannelMessage =
        "OUTPUT channel(s) {0} are not processed by Klacks and would be silently discarded. {1}";

    private const string SupportedChannelsHint =
        "Klacks processes channel 1 (result) and the surcharge channels 10 (night), 11 (weekend day 1), "
        + "12 (weekend day 2), 13 (weekend day 3) and 14 (holiday).";

    private const string ChannelListSeparator = ", ";

    public static string? FindViolation(MacroOutputChannelScan scan)
    {
        if (!scan.Succeeded)
        {
            return string.Format(CultureInfo.InvariantCulture, ScanFailedMessage, scan.FailureMessage);
        }

        if (scan.NonLiteralChannelCount > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, ComputedChannelMessage, SupportedChannelsHint);
        }

        var unsupported = scan.LiteralChannels
            .Where(channel => !MacroOutputChannels.Supported.Contains(channel))
            .Distinct()
            .OrderBy(channel => channel)
            .ToList();

        return unsupported.Count == 0
            ? null
            : string.Format(
                CultureInfo.InvariantCulture,
                UnsupportedChannelMessage,
                string.Join(ChannelListSeparator, unsupported),
                SupportedChannelsHint);
    }
}
