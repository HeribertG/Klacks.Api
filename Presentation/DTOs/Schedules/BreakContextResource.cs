using Klacks.Api.Domain.Common;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BreakContextResource
{
    public Guid Id { get; set; }

    public Guid AbsenceId { get; set; }

    public TimeOnly StartBreak { get; set; }

    public TimeOnly EndBreak { get; set; }

    public MultiLanguage DetailName { get; set; } = null!;
}
