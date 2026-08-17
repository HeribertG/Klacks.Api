// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <param name="UserId">AppUser.Id (text, no FK - see EscalationStage).</param>
/// <param name="DisplayName">Resolved at read time.</param>
/// <param name="HasPhoneNumber">False means this member is silently skipped when the chain is built -
/// shown unfiltered here so the admin can see and fix it.</param>
/// <param name="IsCurrentlyAbsent">True when a UserAbsencePeriod covers today - shown unfiltered here
/// for the same reason.</param>
public readonly record struct EscalationRosterMember(
    string UserId, string DisplayName, bool HasPhoneNumber, bool IsCurrentlyAbsent);
