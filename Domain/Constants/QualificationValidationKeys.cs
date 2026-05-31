// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Translation keys for mandatory-qualification eligibility violations surfaced by the pre-commit
/// guardrail (place_work / propose_plan / cover_absence) and find_replacement. Agent-facing only —
/// these never reach the schedule error-list grid (the eligibility check lives in the pre-commit
/// checker, not the live validator), so no UI i18n entry is required.
/// </summary>
public static class QualificationValidationKeys
{
    public const string Missing = "schedule.error-list.qualification-missing";
    public const string Expired = "schedule.error-list.qualification-expired";
    public const string InsufficientLevel = "schedule.error-list.qualification-level";

    public static string ForReason(QualificationGapReason reason) => reason switch
    {
        QualificationGapReason.Missing => Missing,
        QualificationGapReason.Expired => Expired,
        QualificationGapReason.InsufficientLevel => InsufficientLevel,
        _ => Missing,
    };
}
