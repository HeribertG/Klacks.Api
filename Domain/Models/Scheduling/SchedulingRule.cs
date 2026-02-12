using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Scheduling;

public class SchedulingRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public int? MaxWorkDays { get; set; }

    public int? MinRestDays { get; set; }

    public decimal? MinPauseHours { get; set; }

    public decimal? MaxOptimalGap { get; set; }

    public decimal? MaxDailyHours { get; set; }

    public decimal? MaxWeeklyHours { get; set; }

    public int? MaxConsecutiveDays { get; set; }
}
