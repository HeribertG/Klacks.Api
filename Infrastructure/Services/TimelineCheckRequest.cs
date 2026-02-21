namespace Klacks.Api.Infrastructure.Services;

public class TimelineCheckRequest
{
    public Guid ClientId { get; init; }
    public DateOnly Date { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsRangeCheck { get; init; }
}
