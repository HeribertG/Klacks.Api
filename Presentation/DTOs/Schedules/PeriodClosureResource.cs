namespace Klacks.Api.Presentation.DTOs.Schedules;

public class PeriodClosureResource
{
    public Guid Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ClosedBy { get; set; } = string.Empty;
    public DateTime ClosedAt { get; set; }
}
