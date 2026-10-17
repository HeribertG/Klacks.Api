// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A single unmet qualification of a client for a shift on a date: which qualification,
/// why it is unmet (missing entirely / held but expired / held but below the required level),
/// the minimum level the shift requires, and whether the requirement is mandatory. The default
/// severity rule lives here: a completely missing MANDATORY qualification is an Error (a hard blocker);
/// a MANDATORY qualification held below the required level is an Error only when its level is binding
/// (<see cref="IsLevelBinding"/>, resolved by QualificationLevelBinding), otherwise a Warning — this holds for an
/// in-window row (InsufficientLevel) and for an expired row that is also too low (Expired with LevelDelta above 0);
/// an expired MANDATORY qualification at the required level is a Warning unless <see cref="ExpiredMandatoryBlocks"/>
/// (the opt-in QUALIFICATION_EXPIRED_MANDATORY_BLOCKS setting, default false) promotes it to Error; any optional gap
/// is a Warning.
/// </summary>
/// <param name="QualificationId">The required qualification the client does not satisfy</param>
/// <param name="Reason">Why the requirement is unmet</param>
/// <param name="RequiredMinLevel">The minimum level the shift demands</param>
/// <param name="IsMandatory">Whether the shift marks this requirement mandatory</param>
/// <param name="ExpiredMandatoryBlocks">Opt-in: promote an expired mandatory gap to Error too</param>
/// <param name="HeldLevel">Highest held level: in the validity window for InsufficientLevel, over all held rows for Expired; null for Missing</param>
/// <param name="IsLevelBinding">Effective binding of the minimum level (requirement override or qualification default)</param>
public sealed record QualificationGap(
    Guid QualificationId,
    QualificationGapReason Reason,
    QualificationLevel RequiredMinLevel,
    bool IsMandatory = true,
    bool ExpiredMandatoryBlocks = false,
    QualificationLevel? HeldLevel = null,
    bool IsLevelBinding = false)
{
    /// <summary>Levels missing to the required minimum; 0 for a Missing gap and for a held level at or above it.</summary>
    public int LevelDelta =>
        (Reason is QualificationGapReason.InsufficientLevel or QualificationGapReason.Expired) && HeldLevel.HasValue
            ? Math.Max(0, (int)RequiredMinLevel - (int)HeldLevel.Value)
            : 0;

    public QualificationGapSeverity Severity =>
        IsMandatory
        && (Reason == QualificationGapReason.Missing
            || (Reason == QualificationGapReason.Expired && (ExpiredMandatoryBlocks || (IsLevelBinding && LevelDelta > 0)))
            || (IsLevelBinding && Reason == QualificationGapReason.InsufficientLevel))
            ? QualificationGapSeverity.Error
            : QualificationGapSeverity.Warning;

    /// <summary>
    /// True when the gap is an Error only because the held level is below a binding minimum (in-window, or expired and
    /// also too low without the expired-mandatory escalation): the employee holds the qualification, just too low.
    /// </summary>
    public bool IsBindingLevelBlock =>
        Severity == QualificationGapSeverity.Error
        && (Reason == QualificationGapReason.InsufficientLevel
            || (Reason == QualificationGapReason.Expired && !ExpiredMandatoryBlocks));
}
