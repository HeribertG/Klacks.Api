namespace Klacks.Api.Domain.Models.Schedules;

public class Break : ScheduleEntryBase
{
    public Guid AbsenceId { get; set; }

    public virtual Absence? Absence { get; set; }
}
