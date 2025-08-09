using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Presentation.DTOs.Filter
{
    public class TruncatedAbsence : BaseTruncatedResult
    {
        public ICollection<Absence> Absences { get; set; } = null!;
    }
}
