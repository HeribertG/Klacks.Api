// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class Work : ScheduleEntryBase
{
    [JsonIgnore]
    public virtual Shift? Shift { get; set; } = null!;

    public Guid ShiftId { get; set; }

    [ForeignKey("ParentWork")]
    public Guid? ParentWorkId { get; set; }

    [JsonIgnore]
    public virtual Work? ParentWork { get; set; }
}
