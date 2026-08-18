// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <param name="UserId">AppUser.Id (text, no FK - see EscalationStage).</param>
/// <param name="DisplayName">Resolved at read time.</param>
/// <param name="IsCurrentlyAbsent">True when a UserAbsencePeriod covers today - shown unfiltered here
/// so the admin can manage the absence period of a currently-absent member.</param>
public readonly record struct EscalationRosterMember(
    string UserId, string DisplayName, bool IsCurrentlyAbsent);
