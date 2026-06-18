// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Structured reason an operator rejected an AnalyseScenario. The single genuinely-new datum the
/// future preference-learner needs beyond the ternary status — it tells the learner which dimension
/// drove the rejection. Unspecified is the default for legacy/one-click rejects without a reason.
/// </summary>
public enum RejectReason
{
    Unspecified = 0,
    CoverageDrop = 1,
    HoursImbalance = 2,
    PreferenceViolation = 3,
    QualificationConcern = 4,
    TooMuchChurn = 5,
    Other = 6
}
