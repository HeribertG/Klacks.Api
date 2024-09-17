using Klacks_api.Resources.Schedules;

namespace Klacks_api.Resources.Filter
{
  public class TruncatedAbsence : BaseTruncatedResult
  {
    public ICollection<AbsenceResource> Absences { get; set; } = null!;
  }
}
