using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Resources.Filter
{
  public class TruncatedShift : BaseTruncatedResult
  {
    public ICollection<Shift> Shifts { get; set; } = null!;
  }
}
