using Klacks.Api.Resources.Schedules;

namespace Klacks.Api.Resources.Filter;

public class TruncatedShiftResource : BaseTruncatedResult
{
    public ICollection<ShiftResource> Shifts { get; set; } = null!;
}
