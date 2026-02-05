namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkAddBreaksRequest
{
    public List<BulkBreakItem> Breaks { get; set; } = [];

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }
}
