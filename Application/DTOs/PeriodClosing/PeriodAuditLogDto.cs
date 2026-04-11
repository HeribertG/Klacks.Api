// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// Audit log entry for a seal or unseal action performed on a work period.
/// </summary>
public class PeriodAuditLogDto
{
    public Guid Id { get; set; }

    public PeriodAuditAction Action { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? GroupId { get; set; }

    public string? GroupName { get; set; }

    public string? Reason { get; set; }

    public int AffectedCount { get; set; }

    public DateTime PerformedAt { get; set; }

    public string PerformedBy { get; set; } = string.Empty;
}
