namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkAddWorksRequest
{
    public Guid ShiftId { get; set; }

    public decimal WorkTime { get; set; }

    public List<WorkEntry> Entries { get; set; } = [];
}

public class WorkEntry
{
    public Guid ClientId { get; set; }

    public DateTime CurrentDate { get; set; }
}
