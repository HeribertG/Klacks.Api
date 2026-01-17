using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class BreakContext : BaseEntity
{
    public Guid AbsenceId { get; set; }

    public virtual Absence Absence { get; set; } = null!;

    public TimeOnly StartBreak { get; set; }

    public TimeOnly EndBreak { get; set; }

    public MultiLanguage DetailName { get; set; } = null!;

}
