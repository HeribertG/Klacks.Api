// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the trajectories say about one activated learning artefact inside a time window. Counted in the
/// database rather than materialised, because the only interesting answer is a handful of integers.
/// </summary>
/// <param name="Uses">Turns the artefact was involved in at all</param>
/// <param name="Successes">Turns that reached the intended skill or ran the recipe and were not corrected</param>
/// <param name="Corrections">Turns the user corrected, explicitly or by contradicting the next answer</param>
/// <param name="Helpful">Turns the user gave a thumbs-up</param>
/// <param name="LastUsedAtUtc">Most recent turn in the window, null when there was none</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record LearnedArtefactUsage(
    int Uses, int Successes, int Corrections, int Helpful, DateTime? LastUsedAtUtc)
{
    public static readonly LearnedArtefactUsage None = new(0, 0, 0, 0, null);
}
