// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of judging a user reply as a yes: the verdict plus, for an Ambiguous verdict, the colliding word and the
/// languages whose vocabularies disagree about it, so the caller can tell the model what to ask back.
/// NotConfirmed means "not a confirmation", not "a refusal".
/// </summary>
/// <param name="Verdict">Confirmed, NotConfirmed or Ambiguous</param>
/// <param name="AmbiguousWord">The colliding word that could not be resolved, null unless Ambiguous</param>
/// <param name="AmbiguousLanguages">Every language that lists the word as a yes or as a no, empty unless Ambiguous</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConfirmationAssessment(
    ConfirmationVerdict Verdict,
    string? AmbiguousWord = null,
    IReadOnlyList<string>? AmbiguousLanguages = null)
{
    public static readonly ConfirmationAssessment NotConfirmed = new(ConfirmationVerdict.NotConfirmed);

    public static readonly ConfirmationAssessment Confirmed = new(ConfirmationVerdict.Confirmed);

    public bool IsConfirmed => Verdict == ConfirmationVerdict.Confirmed;

    public bool IsAmbiguous => Verdict == ConfirmationVerdict.Ambiguous;
}
