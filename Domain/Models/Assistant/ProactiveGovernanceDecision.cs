// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The effective governance for one trigger kind in one scope, with the stored rule, the defaults, the
/// global kill switch and the global autonomy level already folded together. EffectiveMaxAction is the
/// only value an action dispatcher needs to read for the UNDELEGATED case: it is already Hint when the
/// kill switch is on or the kind is disabled, and already capped by GlobalAutonomyCap, so no caller has
/// to remember to check those separately. GlobalAutonomyCap is exposed on its own because an Etappe-4e
/// delegation can still raise the requested action past EffectiveMaxAction, and the global level must
/// cap that raise too (Owner decision 2026-08-28) - a caller applying a delegation folds its own raise
/// against GlobalAutonomyCap rather than against EffectiveMaxAction.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ProactiveGovernanceDecision(
    string TriggerKind,
    Guid? GroupId,
    ProactiveMaxAction EffectiveMaxAction,
    ProactiveMaxAction ConfiguredMaxAction,
    bool Enabled,
    bool KillSwitchActive,
    Guid? ResponsibleOwnerUserId,
    int DailyActionBudget,
    int WindowActionLimit,
    int WindowMinutes,
    bool IsStored,
    ProactiveMaxAction GlobalAutonomyCap = ProactiveMaxAction.Execute);
