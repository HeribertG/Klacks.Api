// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Audit record for every seal or unseal action performed on a work period.
/// Reason is mandatory for Unseal, optional for Seal.
/// </summary>
public class PeriodAuditLog : BaseEntity
{
    public PeriodAuditAction Action { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? GroupId { get; set; }

    public string? Reason { get; set; }

    public int AffectedCount { get; set; }

    public DateTime PerformedAt { get; set; }

    public string PerformedBy { get; set; } = string.Empty;
}
