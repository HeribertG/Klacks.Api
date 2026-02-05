using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class TruncatedAbsenceResource : BaseTruncatedResult
{
    public ICollection<AbsenceResource> Absences { get; set; } = null!;
}
