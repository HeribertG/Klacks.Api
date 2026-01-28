namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkBreakItem
{
    public Guid ClientId { get; set; }

    public Guid AbsenceId { get; set; }

    public DateTime CurrentDate { get; set; }

    public decimal WorkTime { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Information { get; set; }
}
