// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the stored PERIOD_CLOSE_LAG_DAYS setting. A missing, unparsable or out-of-range value reads as null
/// ("no lag stored"), which is different from a stored 0: a stored value is the proof that the user was asked
/// when periods are to be closed, so callers that act on it must treat null as "never".
/// </summary>
/// <param name="settingsReader">Source of the setting row</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class PeriodCloseLagReader
{
    public static async Task<int?> ReadAsync(ISettingsReader settingsReader)
    {
        var setting = await settingsReader.GetSetting(SettingKeys.PeriodCloseLagDays);
        if (setting?.Value == null
            || !int.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days)
            || !PeriodCloseDateCalculator.IsValidLag(days))
        {
            return null;
        }

        return days;
    }
}
