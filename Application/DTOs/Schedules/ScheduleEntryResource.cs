using Klacks.Api.Domain.Enums;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ScheduleEntryResource
{
    public Guid Id { get; set; }

    public ClientResource? Client { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly CurrentDate { get; set; }

    public string? Information { get; set; }

    public decimal WorkTime { get; set; }

    public decimal Surcharges { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public WorkLockLevel LockLevel { get; set; }

    public DateTime? SealedAt { get; set; }

    public string? SealedBy { get; set; }

    public PeriodHoursResource? PeriodHours { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public List<WorkScheduleResource> ScheduleEntries { get; set; } = [];
}
