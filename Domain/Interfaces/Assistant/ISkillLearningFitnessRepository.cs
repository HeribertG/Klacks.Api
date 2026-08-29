// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the weekly usefulness snapshots of activated learning artefacts. Self-committing,
/// like the other learning repositories: its only caller is a background pass.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningFitnessRepository
{
    /// <summary>
    /// Writes the counters for one artefact and one calendar week, replacing what an earlier pass of the
    /// same week wrote. The pass runs several times a day and the window is rolling, so a week's row is
    /// a running figure until the week is over rather than a one-off measurement.
    /// </summary>
    Task UpsertAsync(SkillLearningFitness fitness, CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recent snapshot of one artefact, or null while none has been taken.
    /// </summary>
    Task<SkillLearningFitness?> GetLatestAsync(Guid candidateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recent snapshot per artefact for a set of artefacts, for the admin card, which renders a
    /// whole list at once and must not ask per row.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, SkillLearningFitness>> GetLatestForCandidatesAsync(
        IReadOnlyList<Guid> candidateIds, CancellationToken cancellationToken = default);
}
