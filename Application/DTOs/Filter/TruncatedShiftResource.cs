using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedShiftResource : BaseTruncatedResult
{
    public ICollection<ShiftResource> Shifts { get; set; } = null!;
}
