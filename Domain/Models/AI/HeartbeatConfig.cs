using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.AI;

public class HeartbeatConfig : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public int IntervalMinutes { get; set; } = 30;

    public TimeOnly ActiveHoursStart { get; set; } = new(8, 0);

    public TimeOnly ActiveHoursEnd { get; set; } = new(22, 0);

    public string ChecklistJson { get; set; } = "[]";

    public DateTime? LastExecutedAt { get; set; }

    public bool OnboardingCompleted { get; set; }
}
