using Klacks.Api.Presentation.Resources.Schedules;

namespace Klacks.Api.Presentation.Resources.Filter
{
    public class TruncatedAbsenceResource : BaseTruncatedResult
    {
        public ICollection<AbsenceResource> Absences { get; set; } = null!;
    }
}
