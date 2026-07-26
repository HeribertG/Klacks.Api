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
        var keywords = new ScheduleCommandKeywordSet
        {
            FreeToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_FREE, AppSettings.SCHEDULE_COMMAND_KEYWORD_FREE_DEFAULT),
            NegFreeToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_FREE, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_FREE_DEFAULT),
            EarlyToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_EARLY, AppSettings.SCHEDULE_COMMAND_KEYWORD_EARLY_DEFAULT),
            NegEarlyToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_EARLY, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_EARLY_DEFAULT),
            LateToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_LATE, AppSettings.SCHEDULE_COMMAND_KEYWORD_LATE_DEFAULT),
            NegLateToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_LATE, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_LATE_DEFAULT),
            NightToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_NIGHT, AppSettings.SCHEDULE_COMMAND_KEYWORD_NIGHT_DEFAULT),
            NegNightToken = await ResolveAsync(AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_NIGHT, AppSettings.SCHEDULE_COMMAND_KEYWORD_NEG_NIGHT_DEFAULT),
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

    private async Task<string> ResolveAsync(string settingType, string defaultValue)
    {
        var setting = await _settingsReader.GetSetting(settingType);
        return string.IsNullOrWhiteSpace(setting?.Value) ? defaultValue : setting.Value;
    }
}
