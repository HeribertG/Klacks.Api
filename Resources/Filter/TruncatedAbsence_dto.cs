using Klacks_api.Models.Schedules;

namespace Klacks_api.Resources.Filter
{
  public class TruncatedAbsence_dto : BaseTruncatedResult
  {

    public ICollection<Absence> Absences { get; set; } = null!;
  }
}
