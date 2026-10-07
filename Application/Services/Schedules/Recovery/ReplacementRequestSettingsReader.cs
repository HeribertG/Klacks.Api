// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the two runtime settings of the replacement request book, with documented fallbacks (owner decision
/// 2026-10-07): the short-notice threshold (a report less than this many hours before the shift starts is
/// "short notice", judged when reading, never stored) and the retention period after which rows are
/// soft-deleted (24 months, not the general ten years, for works-council reasons in DE/AT). Settings rows, not
/// options, so an operator can change them without a restart; a missing, unparsable or non-positive value
/// falls back.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Settings;
using SettingKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public static class ReplacementRequestSettingsReader
{
    public const int FallbackShortNoticeHours = 48;
    public const int FallbackRetentionDays = 730;

    public static Task<int> ReadShortNoticeHoursAsync(ISettingsReader settingsReader)
        => ReadPositiveIntAsync(settingsReader, SettingKeys.REPLACEMENT_SHORT_NOTICE_HOURS, FallbackShortNoticeHours);

    public static Task<int> ReadRetentionDaysAsync(ISettingsReader settingsReader)
        => ReadPositiveIntAsync(settingsReader, SettingKeys.REPLACEMENT_REQUEST_RETENTION_DAYS, FallbackRetentionDays);

    private static async Task<int> ReadPositiveIntAsync(ISettingsReader settingsReader, string type, int fallback)
    {
        var setting = await settingsReader.GetSetting(type);
        var raw = setting?.Value;

        if (!string.IsNullOrWhiteSpace(raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }

        return fallback;
    }
}
