using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public abstract class ScheduleEntryBase : BaseEntity
{
    [JsonIgnore]
    public virtual Client? Client { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly CurrentDate { get; set; }

    public string? Information { get; set; }

    public decimal WorkTime { get; set; }

    public decimal Surcharges { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public WorkLockLevel LockLevel { get; set; } = WorkLockLevel.None;

    public DateTime? SealedAt { get; set; }

    public string? SealedBy { get; set; }
}
