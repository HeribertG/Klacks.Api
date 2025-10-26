namespace Klacks.Api.Presentation.DTOs.Schedules;

public class ResetDateRangeResponse
{
    public DateOnly EarliestResetDate { get; set; }

    public DateOnly? UntilDate { get; set; }
}
