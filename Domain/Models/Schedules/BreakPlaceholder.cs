// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class BreakPlaceholder : BaseEntity
{
    public Guid AbsenceId { get; set; }

    public virtual Absence Absence { get; set; } = null!;

    public Guid ClientId { get; set; }

    [JsonIgnore]
    public virtual Client Client { get; set; } = null!;

    public DateTime From { get; set; }

    public DateTime Until { get; set; }

    public string? Information { get; set; }
}
