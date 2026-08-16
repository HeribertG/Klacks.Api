// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <param name="UserId">AppUser.Id (text, no FK - see EscalationStage).</param>
/// <param name="DisplayName">Snapshot taken at resolution time, frozen into EscalationStage.UserDisplayName.</param>
public readonly record struct EscalationRosterCandidate(string UserId, string DisplayName);
