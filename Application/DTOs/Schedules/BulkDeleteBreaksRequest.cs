namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkDeleteBreaksRequest
{
    public List<Guid> BreakIds { get; set; } = [];

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }
}
