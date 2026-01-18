using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

public class AbsenceDetail : BaseEntity
{
    public Guid AbsenceId { get; set; }

    public virtual Absence Absence { get; set; } = null!;

    public AbsenceDetailMode Mode { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal Duration { get; set; }

    public MultiLanguage DetailName { get; set; } = null!;
}
