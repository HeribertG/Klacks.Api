// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

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

    /// <summary>
    /// Scenario works only: the work of the clone source (real plan or source scenario) this work stands for. Set by the
    /// scenario clone and carried over when a Wizard 1 work takes over a displaced work's expenses; null on real works
    /// (accept clears it). Lets a harmonizer apply correlate a source scenario's boundary copy with a real-plan clone.
    /// </summary>
    public Guid? ClonedFromWorkId { get; set; }

    public TransportMode? TransportMode { get; set; }

    public string? StartBase { get; set; }

    public string? EndBase { get; set; }

    [JsonIgnore]
    public virtual ICollection<SurchargeItem> SurchargeItems { get; set; } = new List<SurchargeItem>();
}
