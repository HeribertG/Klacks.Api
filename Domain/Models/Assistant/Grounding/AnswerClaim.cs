// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One hard, checkable claim extracted from an assistant answer.
/// </summary>
/// <param name="Kind">Claim class (uuid, number, date).</param>
/// <param name="RawText">The exact text span the claim was extracted from.</param>
/// <param name="Readings">Normalized coverage keys; ambiguous locale formats yield several readings and coverage of ANY reading counts as covered.</param>

namespace Klacks.Api.Domain.Models.Assistant.Grounding;

public sealed record AnswerClaim(
    AnswerClaimKind Kind,
    string RawText,
    IReadOnlyList<string> Readings);
