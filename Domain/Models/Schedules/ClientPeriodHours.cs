// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class ClientPeriodHours : BaseEntity
{
    public Guid ClientId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal Hours { get; set; }

    public decimal Surcharges { get; set; }

    public PaymentInterval PaymentInterval { get; set; } = PaymentInterval.Monthly;

    public Guid? IndividualPeriodId { get; set; }

    public DateTime CalculatedAt { get; set; }

    [JsonIgnore]
    public virtual Client? Client { get; set; }

    [JsonIgnore]
    public virtual IndividualPeriod? IndividualPeriod { get; set; }
}
