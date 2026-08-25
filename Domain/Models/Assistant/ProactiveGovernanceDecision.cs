// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The effective governance for one trigger kind in one scope, with the stored rule, the defaults and
/// the global kill switch already folded together. EffectiveMaxAction is the only value an action
/// dispatcher needs to read: it is already Hint when the kill switch is on or the kind is disabled, so
/// no caller has to remember to check those separately.
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
    bool IsStored);
