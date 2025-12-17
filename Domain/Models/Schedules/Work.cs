using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class Work : BaseEntity
{
    [JsonIgnore]
    public virtual Client Client { get; set; } = null!;

    public Guid ClientId { get; set; }

    public DateTime CurrentDate { get; set; }

    public string? Information { get; set; }

    public bool IsSealed { get; set; }

    [JsonIgnore]
    public virtual Shift? Shift { get; set; } = null!;

    public Guid ShiftId { get; set; }

    public decimal WorkTime { get; set; }
}
