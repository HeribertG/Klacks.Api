// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Derives the coverage status of one replacement slot (shift x date) from the recorded answers of its candidates.
/// A slot is Covered when a candidate accepted or a replacement already reached the plan; otherwise AwaitingAnswer
/// while somebody was asked without a recorded answer; NobodyAvailable when every candidate declined or could not be
/// reached; PartlyDeclined when some candidates declined or could not be reached and the others were only proposed
/// (so the refusal stays visible and nobody is waiting); NotYetAsked when every candidate was only proposed. Proposed
/// never counts as a contact.
/// </summary>
/// <param name="candidates">Outcome of every candidate row of the slot and whether its replacement reached the plan</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Schedules;

public static class ReplacementSlotStatusPolicy
{
    public static ReplacementSlotStatus Classify(IReadOnlyCollection<(ReplacementRequestOutcome Outcome, bool Applied)> candidates)
    {
        if (candidates.Any(c => c.Applied || c.Outcome == ReplacementRequestOutcome.Accepted))
        {
            return ReplacementSlotStatus.Covered;
        }

        if (candidates.Any(c => c.Outcome == ReplacementRequestOutcome.Requested))
        {
            return ReplacementSlotStatus.AwaitingAnswer;
        }

        if (candidates.Any(c => c.Outcome == ReplacementRequestOutcome.Proposed))
        {
            return candidates.Any(c => c.Outcome == ReplacementRequestOutcome.Declined || c.Outcome == ReplacementRequestOutcome.NotReached)
                ? ReplacementSlotStatus.PartlyDeclined
                : ReplacementSlotStatus.NotYetAsked;
        }

        return ReplacementSlotStatus.NobodyAvailable;
    }
}
