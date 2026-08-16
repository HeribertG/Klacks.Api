// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <param name="Id">EscalationRosterEntry.Id, the row an admin reorder targets.</param>
/// <param name="UserId">AppUser.Id (text, no FK - see EscalationStage).</param>
/// <param name="DisplayName">Resolved at read time; not frozen like EscalationRosterCandidate's.</param>
/// <param name="EffectiveRank">OverrideRank ?? DerivedRank; null only for an orphaned entry.</param>
/// <param name="HasOverride">True once an admin has manually placed this entry (OverrideRank set).</param>
/// <param name="IsOrphaned">True when the user lost GroupVisibility but an OverrideRank kept the row (E60).</param>
public readonly record struct EscalationRosterEntryDetail(
    Guid Id, string UserId, string DisplayName, int? EffectiveRank, bool HasOverride, bool IsOrphaned);
