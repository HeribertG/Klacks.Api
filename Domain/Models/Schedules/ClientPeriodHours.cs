using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class ClientPeriodHours : BaseEntity
{
    public Guid ClientId { get; set; }

    public int Year { get; set; }

    public int? Month { get; set; }

    public int? WeekNumber { get; set; }

    public Guid? IndividualPeriodId { get; set; }

    public decimal Hours { get; set; }

    public decimal Surcharges { get; set; }

    public PaymentInterval PaymentInterval { get; set; } = PaymentInterval.Monthly;

    [JsonIgnore]
    public virtual Client? Client { get; set; }

    [JsonIgnore]
    public virtual IndividualPeriod? IndividualPeriod { get; set; }
}
