namespace Klacks.Api.Application.DTOs.Scheduling;

public class SchedulingRuleResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? MaxWorkDays { get; set; }

    public int? MinRestDays { get; set; }

    public decimal? MinPauseHours { get; set; }

    public decimal? MaxOptimalGap { get; set; }

    public decimal? MaxDailyHours { get; set; }

    public decimal? MaxWeeklyHours { get; set; }

    public int? MaxConsecutiveDays { get; set; }
}
