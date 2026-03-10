// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("get_current_time")]
public class GetCurrentTimeSkill : BaseSkillImplementation
{
    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var format = GetParameter<string>(parameters, "format", "full");

        var timezone = context.UserTimezone ?? "Europe/Berlin";
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch
        {
            tz = TimeZoneInfo.Local;
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

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
            Timezone = timezone,
            UtcOffset = tz.GetUtcOffset(now).ToString(),
            DayOfWeek = now.DayOfWeek.ToString(),
            WeekNumber = System.Globalization.ISOWeek.GetWeekOfYear(now)
        };

        return Task.FromResult(SkillResult.SuccessResult(timeData, $"Current time: {formatted}"));
    }
}
