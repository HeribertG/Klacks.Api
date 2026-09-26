// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Whether Klacksy may close a group's period on its own right now, with every brake already folded
/// together. Callers read CanClose and never re-derive it from the other members.
/// </summary>
/// <param name="EffectiveLevel">Minimum of the global proactive level and the admin minimum; Propose when no admin level exists.</param>
/// <param name="DecidingAdminUserId">
/// The admin whose preference holds the admin minimum; the seal is recorded under this id. Only meaningful
/// when CanClose is true - never Guid.Empty.
/// </param>
/// <param name="CanClose">True exactly when BlockedBy is None.</param>
/// <param name="BlockedBy">The strongest brake, None when the close may run.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record PeriodAutoCloseDecision(
    AutonomyLevel EffectiveLevel,
    Guid? DecidingAdminUserId,
    bool CanClose,
    PeriodAutoCloseBlockedBy BlockedBy);
