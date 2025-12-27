using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class MonthlyClientHours : BaseEntity
{
    public Guid ClientId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Hours { get; set; }

    public decimal Surcharges { get; set; }

    [JsonIgnore]
    public virtual Client? Client { get; set; }
}
