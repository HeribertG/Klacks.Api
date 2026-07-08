// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

public class SurchargeItem
{
    public Guid Id { get; set; }

    public SurchargeType Type { get; set; }

    public decimal Amount { get; set; }

    public Guid? WorkId { get; set; }

    [JsonIgnore]
    public Work? Work { get; set; }

    public Guid? BreakId { get; set; }

    [JsonIgnore]
    public Break? Break { get; set; }

    public Guid? WorkChangeId { get; set; }

    [JsonIgnore]
    public WorkChange? WorkChange { get; set; }
}
