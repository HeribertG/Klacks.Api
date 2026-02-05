using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class Work : ScheduleEntryBase
{
    [JsonIgnore]
    public virtual Shift? Shift { get; set; } = null!;

    public Guid ShiftId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public string? ConfirmedBy { get; set; }
}
