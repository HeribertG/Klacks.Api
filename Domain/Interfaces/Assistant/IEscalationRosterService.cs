// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a group's escalation call list purely from GroupVisibility and AppUser.DisplayOrder - no
/// roster-owned persistence. A visible member is skipped from the wake-up order (but never hidden from
/// GetRosterMembersAsync) while a UserAbsencePeriod covers today or while they have no phone number;
/// the global admin role is always appended last as a fixed fallback stage (Owner decision A2).
/// </summary>
/// <param name="groupId">Any group id in the target group's subtree; resolved to its root before lookup.</param>

using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationRosterService
{
    Task<IReadOnlyList<EscalationRosterCandidate>> GetOrderedRosterAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>Admin view of a group's visible members for the roster card, unfiltered by absence or
    /// phone number so the admin can see and fix the reason a member would be skipped.</summary>
    Task<IReadOnlyList<EscalationRosterMember>> GetRosterMembersAsync(Guid groupId, CancellationToken cancellationToken = default);
}
