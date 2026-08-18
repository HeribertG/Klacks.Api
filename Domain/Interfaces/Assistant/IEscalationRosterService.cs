// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a group's escalation call list from GroupVisibility and AppUser.EscalationRosterOrder.
/// A visible member is skipped from the wake-up order while a UserAbsencePeriod covers today or while
/// they have no phone number; the global admin role is always appended last as a fixed fallback stage
/// (Owner decision A2). The admin-facing roster (GetRosterMembersAsync/ReorderAsync) is intentionally
/// NOT group-scoped: it is one flat list of every user who has any GroupVisibility and a phone number,
/// ordered by the same EscalationRosterOrder - a dedicated column, never shared with the user
/// administration list's DisplayOrder (Owner decision, 17.08.).
/// </summary>
/// <param name="groupId">Any group id in the target group's subtree; resolved to its root before lookup.</param>

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationRosterService
{
    Task<IReadOnlyList<EscalationRosterCandidate>> GetOrderedRosterAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Flat, group-agnostic list of every user with any GroupVisibility and a phone number,
    /// for the roster admin card. Unfiltered by absence so the admin can manage absence periods for
    /// currently-absent members too.</summary>
    Task<IReadOnlyList<EscalationRosterMember>> GetRosterMembersAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies an admin's drag'n'drop reorder of the escalation roster's wake-up order.</summary>
    Task<HttpResultResource> ReorderAsync(IReadOnlyList<string> orderedUserIds, CancellationToken cancellationToken = default);
}
