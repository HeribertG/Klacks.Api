using Klacks.Api.Domain.Common;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkBreakItem
{
    public Guid ClientId { get; set; }

    public Guid AbsenceId { get; set; }

    public DateOnly CurrentDate { get; set; }

    public decimal WorkTime { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Information { get; set; }

    public MultiLanguage? Description { get; set; }
}
