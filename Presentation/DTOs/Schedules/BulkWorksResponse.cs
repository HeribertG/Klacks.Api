namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkWorksResponse : BulkScheduleEntryResponse
{
    public List<ShiftDatePair> AffectedShifts { get; set; } = [];
}
