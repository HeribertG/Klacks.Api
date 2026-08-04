// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the scheduling settings the readiness calculation depends on, with the documented fallbacks.
/// Shared by the dashboard query handler and the background email path so both bands are computed from
/// the same configuration instead of two places drifting apart on a changed default.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using SettingKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Schedules;

public static class ResourceMonitorSettingsReader
{
    private const int FallbackMaxWorkDays = 5;
    private const int FallbackMaxConsecutiveDays = 6;

    private const bool DefaultWorkOnMonday = true;
    private const bool DefaultWorkOnTuesday = true;
    private const bool DefaultWorkOnWednesday = true;
    private const bool DefaultWorkOnThursday = true;
    private const bool DefaultWorkOnFriday = true;
    private const bool DefaultWorkOnSaturday = false;
    private const bool DefaultWorkOnSunday = false;

    public static async Task<ResourceMonitorSettings> ReadAsync(
        IResourceMonitorReadRepository readRepository,
        CancellationToken cancellationToken)
    {
        var maxWorkDays = await ReadIntAsync(readRepository, SettingKeys.SCHEDULING_MAX_WORK_DAYS, FallbackMaxWorkDays, cancellationToken);
        var maxConsecutiveDays = await ReadIntAsync(readRepository, SettingKeys.SCHEDULING_MAX_CONSECUTIVE_DAYS, FallbackMaxConsecutiveDays, cancellationToken);

        var pattern = new WeekdayPattern(
            Mon: await ReadBoolAsync(readRepository, SettingKeys.SCHEDULING_DEFAULT_WORK_ON_MONDAY, DefaultWorkOnMonday, cancellationToken),
            Tue: await ReadBoolAsync(readRepository, SettingKeys.SCHEDULING_DEFAULT_WORK_ON_TUESDAY, DefaultWorkOnTuesday, cancellationToken),
            Wed: await ReadBoolAsync(readRepository, SettingKeys.SCHEDULING_DEFAULT_WORK_ON_WEDNESDAY, DefaultWorkOnWednesday, cancellationToken),
            Thu: await ReadBoolAsync(readRepository, SettingKeys.SCHEDULING_DEFAULT_WORK_ON_THURSDAY, DefaultWorkOnThursday, cancellationToken),
            Fri: await ReadBoolAsync(readRepository, SettingKeys.SCHEDULING_DEFAULT_WORK_ON_FRIDAY, DefaultWorkOnFriday, cancellationToken),
            Sat: await ReadBoolAsync(readRepository, SettingKeys.SCHEDULING_DEFAULT_WORK_ON_SATURDAY, DefaultWorkOnSaturday, cancellationToken),
            Sun: await ReadBoolAsync(readRepository, SettingKeys.SCHEDULING_DEFAULT_WORK_ON_SUNDAY, DefaultWorkOnSunday, cancellationToken));

        return new ResourceMonitorSettings(maxWorkDays, maxConsecutiveDays, pattern);
    }

    private static async Task<int> ReadIntAsync(
        IResourceMonitorReadRepository readRepository, string type, int fallback, CancellationToken cancellationToken)
    {
        var raw = await readRepository.GetSettingValue(type, cancellationToken);

        if (!string.IsNullOrWhiteSpace(raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }

        return fallback;
    }

    private static async Task<bool> ReadBoolAsync(
        IResourceMonitorReadRepository readRepository, string type, bool fallback, CancellationToken cancellationToken)
    {
        var raw = await readRepository.GetSettingValue(type, cancellationToken);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return bool.TryParse(raw, out var parsed) ? parsed : fallback;
    }
}
