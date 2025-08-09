using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class TruncatedShiftResource : BaseTruncatedResult
{
    public ICollection<ShiftResource> Shifts { get; set; } = null!;
}
