// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the eight SCHEDULE_COMMAND_KEYWORD_* settings and falls back to the English default
/// token for any key that is unset or blank.
/// </summary>

using AppSettings = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ScheduleCommandKeywordProvider : IScheduleCommandKeywordProvider
{
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<ScheduleCommandKeywordProvider> _logger;

    public ScheduleCommandKeywordProvider(ISettingsReader settingsReader, ILogger<ScheduleCommandKeywordProvider> logger)
    {
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<ScheduleCommandKeywordSet> GetAsync(CancellationToken cancellationToken = default)
    {
        var resolved = await _settingsReader.GetSettingsByTypesAsync(
        [
            AppSettings.SCHEDULE_COMMAND_KEYWORD_FREE,
            AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_FREE,
            AppSettings.SCHEDULE_COMMAND_KEYWORD_EARLY,
            AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_EARLY,
            AppSettings.SCHEDULE_COMMAND_KEYWORD_LATE,
            AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_LATE,
            AppSettings.SCHEDULE_COMMAND_KEYWORD_NIGHT,
            AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_NIGHT,
        ]);
        string Resolve(string settingType, string defaultValue) =>
            resolved.TryGetValue(settingType, out var value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

        var keywords = new ScheduleCommandKeywordSet
        {
            FreeToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_FREE, AppSettings.SCHEDULE_COMMAND_KEYWORD_FREE_DEFAULT),
            NegFreeToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_FREE, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_FREE_DEFAULT),
            EarlyToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_EARLY, AppSettings.SCHEDULE_COMMAND_KEYWORD_EARLY_DEFAULT),
            NegEarlyToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_EARLY, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_EARLY_DEFAULT),
            LateToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_LATE, AppSettings.SCHEDULE_COMMAND_KEYWORD_LATE_DEFAULT),
            NegLateToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_LATE, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_LATE_DEFAULT),
            NightToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_NIGHT, AppSettings.SCHEDULE_COMMAND_KEYWORD_NIGHT_DEFAULT),
            NegNightToken = Resolve(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_NIGHT, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_NIGHT_DEFAULT),
        };

        const int expectedTokenCount = 8;
        if (keywords.ValidTokens.Count < expectedTokenCount)
        {
            _logger.LogWarning(
                "Two or more SCHEDULE_COMMAND_KEYWORD_* settings resolve to the same token — only {Count} of {Expected} " +
                "planning commands are reachable. Configured set: {Keywords}",
                keywords.ValidTokens.Count, expectedTokenCount, keywords);
        }

        return keywords;
    }
}
