using Klacks_api.Models.Schedules;

namespace Klacks_api.Resources.Filter
{
  public class TruncatedShift : BaseTruncatedResult
  {
    public ICollection<Shift> Shifts { get; set; } = null!;
  }
}
