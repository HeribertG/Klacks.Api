// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The outcomes a person can record for a replacement request. Proposed is the engine's state before anybody
/// was asked and is never an answer.
/// </summary>
/// <param name="outcome">Outcome to classify</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class ReplacementRequestAnswers
{
    public static readonly IReadOnlyList<ReplacementRequestOutcome> All =
    [
        ReplacementRequestOutcome.Requested,
        ReplacementRequestOutcome.Accepted,
        ReplacementRequestOutcome.Declined,
        ReplacementRequestOutcome.NotReached,
    ];

    public static bool IsAnswer(ReplacementRequestOutcome outcome) => All.Contains(outcome);
}
