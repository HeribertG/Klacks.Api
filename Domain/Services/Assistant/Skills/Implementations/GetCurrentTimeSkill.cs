// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports the current date/time in the caller's time zone: an explicit SkillExecutionContext.UserTimezone
/// wins, an invalid or absent one falls through to the company's own configured time zone (never the
/// server's local zone, never a hard-coded regional default) - Klacks installs internationally and has
/// exactly one configured company zone.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("get_current_time")]
public class GetCurrentTimeSkill : BaseSkillImplementation
{
    private readonly IEffectiveTimeZoneResolver _timeZoneResolver;
    private readonly TimeProvider _timeProvider;

    public GetCurrentTimeSkill(IEffectiveTimeZoneResolver timeZoneResolver, TimeProvider timeProvider)
    {
        _timeZoneResolver = timeZoneResolver;
        _timeProvider = timeProvider;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var format = GetParameter<string>(parameters, "format", "full");

        var (tz, timezoneId) = await _timeZoneResolver.ResolveAsync(context.UserTimezone, cancellationToken);
        var now = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), tz).DateTime;

        var formatted = format?.ToLower() switch
        {
            "date" => now.ToString("yyyy-MM-dd"),
            "time" => now.ToString("HH:mm:ss"),
            "iso" => now.ToString("o"),
            _ => now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        var timeData = new
        {
            DateTime = formatted,
            Timezone = timezoneId,
            UtcOffset = tz.GetUtcOffset(now).ToString(),
            DayOfWeek = now.DayOfWeek.ToString(),
            WeekNumber = System.Globalization.ISOWeek.GetWeekOfYear(now)
        };

        return SkillResult.SuccessResult(timeData, $"Current time: {formatted}");
    }
}
