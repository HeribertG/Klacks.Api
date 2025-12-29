using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public abstract class ScheduleEntryBase : BaseEntity
{
    [JsonIgnore]
    public virtual Client? Client { get; set; }

    public Guid ClientId { get; set; }

    public DateTime CurrentDate { get; set; }

    public string? Information { get; set; }

    public bool IsSealed { get; set; }

    public decimal WorkTime { get; set; }

    public decimal Surcharges { get; set; }

    public TimeOnly StartShift { get; set; }

    public TimeOnly EndShift { get; set; }
}
