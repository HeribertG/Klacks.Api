// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the per-stage approval window an approval chain without a natural deadline is granted
/// (PROACTIVE_APPROVAL_WINDOW_MINUTES), a runtime Settings row like the escalation time-budget caps so
/// an operator can widen it without a restart. Anything missing, unparsable or non-positive falls back
/// to the built-in default rather than to a zero-length window nobody could answer within.
/// </summary>
/// <param name="settingsReader">Source of the Settings row.</param>

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Settings;
using SettingKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public static class ProactiveApprovalWindowReader
{
    public const int DefaultWindowMinutes = 30;

    public static async Task<int> ReadMinutesAsync(ISettingsReader settingsReader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var setting = await settingsReader.GetSetting(SettingKeys.PROACTIVE_APPROVAL_WINDOW_MINUTES);
        var raw = setting?.Value;

        if (!string.IsNullOrWhiteSpace(raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }

        return DefaultWindowMinutes;
    }
}
