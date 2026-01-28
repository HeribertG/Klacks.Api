using Klacks.Api.Domain.Common;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BreakResource : ScheduleEntryResource
{
    public Guid AbsenceId { get; set; }

    public MultiLanguage? Description { get; set; }
}
