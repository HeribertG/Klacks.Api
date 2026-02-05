namespace Klacks.Api.Application.DTOs.Schedules;

public class ResetDateRangeResponse
{
    public DateOnly EarliestResetDate { get; set; }

    public DateOnly? UntilDate { get; set; }
}
