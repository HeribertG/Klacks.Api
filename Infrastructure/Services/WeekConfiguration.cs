// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the CALENDAR_WEEKEND_DAYS / CALENDAR_WEEK_START_DAY settings and falls back to Saturday/Sunday
/// and Monday when unset, so existing installs keep their historical behavior until they opt into a
/// different weekend/week-start configuration (e.g. Friday/Saturday weekends for Gulf-region operators).
/// @param settingsReader - reads the CALENDAR_WEEKEND_DAYS / CALENDAR_WEEK_START_DAY settings
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services;

public class WeekConfiguration : IWeekConfiguration
{
    private static readonly IReadOnlySet<DayOfWeek> DefaultWeekendDays =
        new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };

    private const DayOfWeek DefaultWeekStartDay = DayOfWeek.Monday;

    private readonly ISettingsReader _settingsReader;

    public WeekConfiguration(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<IReadOnlySet<DayOfWeek>> GetWeekendDaysAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.WeekendDays);
        if (string.IsNullOrWhiteSpace(setting?.Value))
        {
            return DefaultWeekendDays;
        }

        var days = setting.Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseDayOfWeek)
            .Where(day => day.HasValue)
            .Select(day => day!.Value)
            .ToHashSet();

        return days;
    }

    public async Task<bool> IsWeekendAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        var weekendDays = await GetWeekendDaysAsync(cancellationToken);
        return weekendDays.Contains(dayOfWeek);
    }

    public async Task<DayOfWeek> GetWeekStartDayAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.WeekStartDay);
        return ParseDayOfWeek(setting?.Value) ?? DefaultWeekStartDay;
    }

    public async Task<DateOnly> GetWeekStartAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var weekStartDay = await GetWeekStartDayAsync(cancellationToken);
        var offset = ((int)date.DayOfWeek - (int)weekStartDay + 7) % 7;
        return date.AddDays(-offset);
    }

    private static DayOfWeek? ParseDayOfWeek(string? value) =>
        Enum.TryParse<DayOfWeek>(value, ignoreCase: true, out var day) ? day : null;
}
