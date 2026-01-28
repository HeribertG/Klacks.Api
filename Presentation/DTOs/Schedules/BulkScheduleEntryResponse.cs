namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkScheduleEntryResponse
{
    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public List<Guid> CreatedIds { get; set; } = [];

    public List<Guid> DeletedIds { get; set; } = [];

    public Dictionary<Guid, PeriodHoursResource>? PeriodHours { get; set; }
}
