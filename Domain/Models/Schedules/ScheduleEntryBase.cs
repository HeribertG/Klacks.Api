// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    /// <summary>
    /// Lock level the entry had right before a period seal raised it to Closed; the period unseal writes it
    /// back. Null means no seal state was recorded (entries sealed before the column existed, or never
    /// period-sealed) and the unseal falls back to None.
    /// </summary>
    [JsonIgnore]
    public WorkLockLevel? PreSealLockLevel { get; set; }

    /// <summary>SealedAt as it was right before the period seal; restored by the period unseal.</summary>
    [JsonIgnore]
    public DateTime? PreSealSealedAt { get; set; }

    /// <summary>SealedBy as it was right before the period seal; restored by the period unseal.</summary>
    [JsonIgnore]
    public string? PreSealSealedBy { get; set; }

    public Guid? AnalyseToken { get; set; }
}
