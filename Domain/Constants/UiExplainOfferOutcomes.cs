// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What happened to a UI explain offer, stored as "outcome" in the counter row's ContentParamsJson: shown (the toast
/// appeared), accepted (yes - Klacksy explains), later (ask again another time) or declined (no, thanks). A toast that
/// is closed or pushed away without a choice sends nothing.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class UiExplainOfferOutcomes
{
    public const string Shown = "shown";

    public const string Accepted = "accepted";

    public const string Later = "later";

    public const string Declined = "declined";

    public static readonly IReadOnlyList<string> All = [Shown, Accepted, Later, Declined];

    /// <summary>True for one of the four known outcomes (case-sensitive, as the UI sends them).</summary>
    /// <param name="outcome">Outcome as sent by the UI</param>
    public static bool IsKnown(string? outcome) => outcome != null && All.Contains(outcome, StringComparer.Ordinal);
}
