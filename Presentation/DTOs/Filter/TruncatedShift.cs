using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Presentation.DTOs.Filter;

public class TruncatedShift : BaseTruncatedResult
{
    public ICollection<Shift> Shifts { get; set; } = null!;
}
