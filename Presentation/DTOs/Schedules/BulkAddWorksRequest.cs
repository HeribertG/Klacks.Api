namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkAddWorksRequest
{
    public List<BulkWorkItem> Works { get; set; } = [];

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }
}

public class BulkWorkItem
{
    public Guid ClientId { get; set; }

    public Guid ShiftId { get; set; }

    public DateOnly CurrentDate { get; set; }

    public decimal WorkTime { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
