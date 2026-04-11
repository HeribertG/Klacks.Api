// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Audit record for every seal or unseal action performed on a work period.
/// Reason is mandatory for Unseal, optional for Seal.
/// </summary>
/// <remarks>
/// PerformedAt and PerformedBy are deliberately kept separate from BaseEntity.CreateTime
/// and CurrentUserCreated: the audit moment is a domain concept that must remain stable
/// even if the row-insert infrastructure later changes its semantics (batching, retries,
/// snapshot restores). Do not remove these fields in favour of the inherited ones.
/// </remarks>
public class PeriodAuditLog : BaseEntity
{
    public PeriodAuditAction Action { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? GroupId { get; set; }

    [MaxLength(2000)]
    public string? Reason { get; set; }

    public int AffectedCount { get; set; }

    public DateTime PerformedAt { get; set; }

    [MaxLength(256)]
    public string PerformedBy { get; set; } = string.Empty;
}
