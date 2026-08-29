// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for persisting skill selection trajectories captured during chat turns.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillSelectionTrajectoryRepository
{
    Task AddAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default);

    Task<SkillSelectionTrajectory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default);

    Task<List<SkillSelectionTrajectory>> GetRecentAsync(Guid agentId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wrong-skill trajectories the description optimizer has not consumed yet, newest first. Evidence a
    /// proposal was already built from is excluded, so a sharpening cannot keep re-proposing itself from
    /// the same handful of corrections on every run. Filtered to CorrectionTypes.WrongSkill in the query
    /// itself, so an implicit correction can never occupy a slot in this window without ever being stamped.
    /// </summary>
    Task<List<SkillSelectionTrajectory>> GetUncorrectedWrongSkillAsync(Guid agentId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps the consumption watermark on the given trajectories. Written as one conditional statement so
    /// two overlapping runs cannot both claim the same evidence.
    /// </summary>
    Task MarkSharpenedAsync(
        IReadOnlyList<Guid> ids, DateTime sharpenedAtUtc, CancellationToken cancellationToken = default);

    Task<SkillSelectionTrajectory?> FindMostRecentByUserAndHashAsync(string userId, string userMessageHash, CancellationToken cancellationToken = default);

    Task<SkillSelectionTrajectory?> FindMostRecentByAgentAndUserAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usage of a learned phrase inside a window, matched through the owner recorded at capture time.
    /// The attribution is per owning skill, not per wording: the capture stores which skill's phrase
    /// occurred, so two learned wordings for the same skill share one set of counters.
    /// </summary>
    /// <param name="ownerName">Skill the learned phrase belongs to</param>
    /// <param name="fromUtc">Inclusive lower bound of the window</param>
    Task<LearnedArtefactUsage> CountPhraseUsageAsync(
        string ownerName, DateTime fromUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usage of a learned capability inside a window, matched through the recipe that forced the turn.
    /// A success is a turn that executed something and was not corrected - deliberately not "the last
    /// step of the recipe reported success", because nothing links a usage row to a turn except a
    /// session id and a time window, and that join would invent a precision this measurement lacks.
    /// </summary>
    /// <param name="recipeName">Name of the learned recipe</param>
    /// <param name="fromUtc">Inclusive lower bound of the window</param>
    Task<LearnedArtefactUsage> CountRecipeUsageAsync(
        string recipeName, DateTime fromUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a learned capability has ever run in a turn nobody corrected. That is what clears the
    /// "first real use still owed" mark the execution oracle leaves on a capability it could not run end
    /// to end.
    /// </summary>
    Task<bool> HasSuccessfulRecipeTurnAsync(string recipeName, CancellationToken cancellationToken = default);
}
