using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ScheduleEntryResource
{
    public Guid Id { get; set; }

    public ClientResource? Client { get; set; }

    public Guid ClientId { get; set; }

    public DateTime CurrentDate { get; set; }

    public string? Information { get; set; }

    public bool IsSealed { get; set; }

    public decimal WorkTime { get; set; }

    public decimal Surcharges { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public PeriodHoursResource? PeriodHours { get; set; }

    public DateOnly? PeriodStart { get; set; }

    public DateOnly? PeriodEnd { get; set; }

    public List<WorkScheduleResource> ScheduleEntries { get; set; } = [];
}
