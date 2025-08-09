using Klacks.Api.Presentation.Resources.Schedules;

namespace Klacks.Api.Presentation.Resources.Filter;

public class TruncatedShiftResource : BaseTruncatedResult
{
    public ICollection<ShiftResource> Shifts { get; set; } = null!;
}
