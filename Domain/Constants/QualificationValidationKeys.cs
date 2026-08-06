// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Translation keys for mandatory-qualification eligibility violations. They are raised by the
/// pre-commit guardrail (place_work / propose_plan / cover_absence) and find_replacement, AND by the
/// live validator, which runs its own qualification check on both the single and the range path —
/// so these keys do reach the schedule error-list grid and do need UI i18n entries.
/// </summary>
public static class QualificationValidationKeys
{
    public const string Missing = "schedule.error-list.qualification-missing";
    public const string Expired = "schedule.error-list.qualification-expired";
    public const string InsufficientLevel = "schedule.error-list.qualification-level";
    public const string ExpiringSoon = "schedule.error-list.qualification-expiring-soon";

    public static string ForReason(QualificationGapReason reason) => reason switch
    {
        QualificationGapReason.Missing => Missing,
        QualificationGapReason.Expired => Expired,
        QualificationGapReason.InsufficientLevel => InsufficientLevel,
        _ => Missing,
    };
}
