using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedAbsenceResource : BaseTruncatedResult
{
    public ICollection<AbsenceResource> Absences { get; set; } = null!;
}
