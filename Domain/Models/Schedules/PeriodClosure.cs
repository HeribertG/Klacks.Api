using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class PeriodClosure : BaseEntity
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string ClosedBy { get; set; } = string.Empty;

    public DateTime ClosedAt { get; set; }
}
