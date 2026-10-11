// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The WorkChange types that record time of the work's own employee (correction, travel, briefing) rather than hand
/// the work to someone else. A work carrying one is a corrected work: the planning wizards treat it as fixed (like a
/// sealed work) so the recorded time is never moved to another employee or deleted. Single definition for every
/// reader; HourBearing are the types the period hours add to the work (briefing/debriefing add none).
/// </summary>
/// <param name="type">The WorkChange type to classify</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class CorrectionWorkChangeTypes
{
    public static readonly IReadOnlyList<WorkChangeType> All = Enum.GetValues<WorkChangeType>()
        .Where(type => !ReplacementWorkChangeTypes.IsReplacement(type))
        .ToList();

    public static readonly IReadOnlyList<WorkChangeType> HourBearing =
    [
        WorkChangeType.CorrectionEnd,
        WorkChangeType.CorrectionStart,
        WorkChangeType.TravelStart,
        WorkChangeType.TravelEnd,
        WorkChangeType.TravelWithin,
    ];

    public static bool IsCorrection(WorkChangeType type) => All.Contains(type);

    public static bool IsHourBearing(WorkChangeType type) => HourBearing.Contains(type);
}
