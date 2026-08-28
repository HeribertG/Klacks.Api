// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one round of phrase learning produced for a cluster. Failure carries the text of the failure,
/// because that text is what the next round's prompt is seeded with.
/// </summary>
/// <param name="PhraseId">Id of the activated skill_phrase row, null when nothing was activated</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record PhraseLearningOutcome(bool Learned, Guid? PhraseId, string? Phrase, string? Error)
{
    public static PhraseLearningOutcome Success(Guid phraseId, string phrase) => new(true, phraseId, phrase, null);

    public static PhraseLearningOutcome Failure(string error) => new(false, null, null, error);
}
