using Klacks.Api.Resources.Schedules;

namespace Klacks.Api.Resources.Filter
{
    public class TruncatedAbsenceResource : BaseTruncatedResult
    {
        public ICollection<AbsenceResource> Absences { get; set; } = null!;
    }
}
