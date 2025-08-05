using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Resources.Filter
{
    public class TruncatedAbsence : BaseTruncatedResult
    {
        public ICollection<Absence> Absences { get; set; } = null!;
    }
}
