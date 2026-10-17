// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How a pending explain offer was settled, stored as "outcome" in the counter row's ContentParamsJson. The reaction
/// column only knows Helpful/Dismissed, so the outcome is what separates a user who simply answered the gate
/// (Proceeded, Declined) from one who ignored the offer (Other, Expired) - only the latter two count towards the long
/// snooze, and the measurement must not read a plain "yes" at the gate as a rejected offer.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ExplainOfferOutcomes
{
    public const string Accepted = "accepted";

    public const string Proceeded = "proceeded";

    public const string Declined = "declined";

    public const string Other = "other";

    public const string Expired = "expired";

    /// <summary>True for the outcomes that mean the offer was ignored; a missing outcome counts as ignored.</summary>
    /// <param name="outcome">The stored outcome, null when the row carries none</param>
    public static bool IsIgnored(string? outcome) =>
        outcome == null
        || string.Equals(outcome, Other, StringComparison.Ordinal)
        || string.Equals(outcome, Expired, StringComparison.Ordinal);
}
