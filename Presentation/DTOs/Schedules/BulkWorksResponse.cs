namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkWorksResponse
{
    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public List<Guid> CreatedIds { get; set; } = [];

    public List<Guid> DeletedIds { get; set; } = [];

    public List<ShiftDatePair> AffectedShifts { get; set; } = [];

    public Dictionary<Guid, PeriodHoursResource>? PeriodHours { get; set; }
}

public class ShiftDatePair
{
    public Guid ShiftId { get; set; }

    public DateTime Date { get; set; }
}
