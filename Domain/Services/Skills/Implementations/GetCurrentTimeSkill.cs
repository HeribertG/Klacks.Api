using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Domain.Services.Skills.Implementations;

public class GetCurrentTimeSkill : BaseSkill
{
    public override string Name => "get_current_time";
    public override string Description => "Get the current date and time in the user's timezone";
    public override SkillCategory Category => SkillCategory.System;

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "format",
            "The format for the date/time output",
            SkillParameterType.Enum,
            Required: false,
            DefaultValue: "full",
            EnumValues: new List<string> { "full", "date", "time", "iso" })
    };

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
