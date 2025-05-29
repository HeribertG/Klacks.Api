using Klacks.Api.Resources.Schedules;

namespace Klacks.Api.Resources.Filter
{
    public class TruncatedAbsence : BaseTruncatedResult
    {
        public ICollection<AbsenceResource> Absences { get; set; } = null!;
    }
}
