// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Audit record for every seal, unseal, day approval or entry confirmation performed on work data.
/// Reason is mandatory for Unseal, optional for every other action.
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

    /// <summary>
    /// Display name of the actor as it was at the moment of the action. AppUser is the only entity
    /// without soft delete and is removed hard, so PerformedBy would otherwise decay into a GUID
    /// nobody can resolve. This is a snapshot on purpose and is never re-resolved by a join.
    /// </summary>
    [MaxLength(256)]
    public string? PerformedByName { get; set; }

    /// <summary>
    /// Typed reference to the approving account, complementing the name snapshot rather than
    /// replacing it: the reference makes it checkable whether the approver still exists and is
    /// active, the name survives the hard deletion that breaks the reference. Deliberately without
    /// a foreign key - see UserAbsencePeriod for the opposite case.
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Builds a record for one action. Keeps the actor mapping in one place: PerformedBy carries the
    /// raw claim value verbatim, ApprovedByUserId only gets a value when that claim really is a GUID,
    /// so a fallback marker never turns into a fake reference.
    /// </summary>
    /// <param name="action">Which action is being recorded</param>
    /// <param name="startDate">First affected day; equals endDate for day-level actions</param>
    /// <param name="endDate">Last affected day</param>
    /// <param name="groupId">Group the action was scoped to, null for all groups</param>
    /// <param name="reason">Free-text justification, mandatory for Unseal</param>
    /// <param name="affectedCount">Number of rows the action changed</param>
    /// <param name="performedBy">Raw NameIdentifier claim value of the actor</param>
    /// <param name="performedByName">Display name of the actor at the moment of the action</param>
    public static PeriodAuditLog For(
        PeriodAuditAction action,
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId,
        string? reason,
        int affectedCount,
        string performedBy,
        string? performedByName)
    {
        return new PeriodAuditLog
        {
            Action = action,
            StartDate = startDate,
            EndDate = endDate,
            GroupId = groupId,
            Reason = reason,
            AffectedCount = affectedCount,
            PerformedAt = DateTime.UtcNow,
            PerformedBy = performedBy,
            PerformedByName = performedByName,
            ApprovedByUserId = Guid.TryParse(performedBy, out var actorId) ? actorId : null
        };
    }
}
