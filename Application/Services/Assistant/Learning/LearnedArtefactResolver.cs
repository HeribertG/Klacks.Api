// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns the clusters that ended in a learned artefact into a uniform list the fitness pass, the pruner
/// and the admin card can all walk. Both of those need exactly the same three facts about an artefact -
/// what it is called, when it went live, and which candidate its snapshots hang off - and neither the
/// cluster nor the candidate carries all three on its own, so resolving it twice in two services would
/// be two chances to resolve it differently.
/// A cluster whose outcome reference no longer resolves is dropped rather than reported: an
/// administrator who deleted a learned phrase should not keep seeing it measured.
/// </summary>
/// <param name="clusterRepository">Clusters that ended in a learned phrase or capability</param>
/// <param name="candidateRepository">Supplies the candidate the fitness rows are keyed by</param>
/// <param name="phraseRepository">Resolves a phrase outcome reference to the skill it belongs to</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class LearnedArtefactResolver : ILearnedArtefactResolver
{
    private static readonly JsonSerializerOptions ProbeJsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private static readonly IReadOnlyList<string> LearnedStatuses =
    [
        SkillLearningClusterStatuses.LearnedPhrase,
        SkillLearningClusterStatuses.LearnedCapability
    ];

    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ISkillLearningCandidateRepository _candidateRepository;
    private readonly ISkillPhraseRepository _phraseRepository;

    public LearnedArtefactResolver(
        ISkillLearningClusterRepository clusterRepository,
        ISkillLearningCandidateRepository candidateRepository,
        ISkillPhraseRepository phraseRepository)
    {
        _clusterRepository = clusterRepository;
        _candidateRepository = candidateRepository;
        _phraseRepository = phraseRepository;
    }

    public async Task<IReadOnlyList<LearnedArtefact>> ListActiveAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        var clusters = await _clusterRepository.ListByStatusAsync(LearnedStatuses, limit, cancellationToken);
        var artefacts = new List<LearnedArtefact>();

        foreach (var cluster in clusters)
        {
            var artefact = await ResolveAsync(cluster, cancellationToken);
            if (artefact != null)
            {
                artefacts.Add(artefact);
            }
        }

        return artefacts;
    }

    private async Task<LearnedArtefact?> ResolveAsync(
        SkillLearningCluster cluster, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cluster.OutcomeRef))
        {
            return null;
        }

        var candidates = await _candidateRepository.ListByClusterAsync(cluster.Id, cancellationToken);
        var candidate = candidates.FirstOrDefault(c => c.Status == SkillLearningCandidateStatuses.Active);

        var activatedAt = cluster.LearnedAtUtc ?? cluster.StatusChangedAtUtc;

        if (string.Equals(cluster.OutcomeRefKind, SkillLearningOutcomeKinds.Capability, StringComparison.Ordinal))
        {
            return new LearnedArtefact(
                cluster.Id,
                SkillLearningOutcomeKinds.Capability,
                cluster.OutcomeRef,
                PhraseId: null,
                candidate?.Id,
                activatedAt,
                IsExecutionUnproven(candidate));
        }

        if (!Guid.TryParse(cluster.OutcomeRef, out var phraseId))
        {
            return null;
        }

        var phrase = await _phraseRepository.GetByIdAsync(phraseId, cancellationToken);
        return phrase == null
            ? null
            : new LearnedArtefact(
                cluster.Id,
                SkillLearningOutcomeKinds.Phrase,
                phrase.OwnerName,
                phraseId,
                candidate?.Id,
                activatedAt,
                ExecutionUnproven: false);
    }

    // The execution oracle's own verdict, read back rather than recomputed. A capability whose steps all
    // ran during the probe owes nothing; one that contains a write, or a step fed by an earlier step's
    // result, was activated on its static proof alone.
    private static bool IsExecutionUnproven(SkillLearningCandidate? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate?.ExecutionResultJson))
        {
            return false;
        }

        try
        {
            var probe = JsonSerializer.Deserialize<SkillExecutionProbe>(
                candidate.ExecutionResultJson, ProbeJsonOptions);

            return probe != null && !probe.FullyExecuted;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
