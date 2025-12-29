using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class Break : ScheduleEntryBase
{
    [JsonIgnore]
    public virtual BreakReason? BreakReason { get; set; } = null!;

    public Guid BreakReasonId { get; set; }
}
