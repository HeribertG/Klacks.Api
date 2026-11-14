// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill-graph association: the graph neighbours of the retrieved skills compete for the last retrieval places
/// instead of waiting for free budget. Every active relation counts, whatever its type and in both directions,
/// weighted by its confidence; a neighbour is eligible only when the retrieval's association pool scored it, and it is
/// ranked by AssociationGraphWeight * (best seed score * edge confidence) + (1 - weight) * its own cosine score. A small
/// exploration probability skips the step for a turn, which keeps an unbiased control stream for the learner.
/// </summary>
/// <param name="repository">Source of the skill-relationship edges for the agent.</param>
/// <param name="logger">Diagnostic logging.</param>
/// <param name="options">Exploration and slot settings; null keeps the shipped SkillGraphConstants defaults</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public class SkillRetrievalExpander : ISkillRetrievalExpander
{
    private readonly ISkillRelationRepository _repository;
    private readonly ILogger<SkillRetrievalExpander> _logger;
    private readonly double _explorationEpsilon;

    public SkillRetrievalExpander(
        ISkillRelationRepository repository,
        ILogger<SkillRetrievalExpander> logger,
        SkillGraphExpansionOptions? options = null)
    {
        _repository = repository;
        _logger = logger;
        var resolved = options ?? SkillGraphExpansionOptions.Default;
        _explorationEpsilon = Math.Clamp(resolved.ExplorationEpsilon, 0, 1);
        AssociationSlots = Math.Max(0, resolved.AssociationSlots);
    }

    public int AssociationSlots { get; }

    public async Task<IReadOnlyList<string>> AssociateAsync(
        Guid agentId,
        IReadOnlyDictionary<string, double> seedScores,
        IReadOnlyDictionary<string, double> poolScores,
        int slots,
        CancellationToken cancellationToken = default)
    {
        if (slots <= 0 || seedScores.Count == 0 || poolScores.Count == 0)
        {
            return [];
        }

        if (_explorationEpsilon > 0 && Random.Shared.NextDouble() < _explorationEpsilon)
        {
            return [];
        }

        var edges = await _repository.GetByAgentAsync(agentId, cancellationToken);
        var associated = RankAssociations(edges, seedScores, poolScores, slots);
        if (associated.Count > 0)
        {
            _logger.LogDebug("Skill association added {Skills}", string.Join(", ", associated));
        }

        return associated;
    }

    /// <summary>
    /// The pure ranking behind AssociateAsync, separated so it can be tested without a repository.
    /// </summary>
    /// <param name="edges">The agent's skill relations; only active ones count</param>
    /// <param name="seedScores">The skills whose neighbours are looked for, with their scores</param>
    /// <param name="poolScores">The eligible candidates with their own scores; a neighbour outside it is ignored</param>
    /// <param name="slots">The number of names to return at most</param>
    public static IReadOnlyList<string> RankAssociations(
        IReadOnlyList<SkillRelation> edges,
        IReadOnlyDictionary<string, double> seedScores,
        IReadOnlyDictionary<string, double> poolScores,
        int slots)
    {
        if (slots <= 0)
        {
            return [];
        }

        var graphEvidence = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            if (edge.Status != SkillRelationStatus.Active)
            {
                continue;
            }

            AddEvidence(graphEvidence, edge.SkillAName, edge.SkillBName, edge.Confidence, seedScores, poolScores);
            AddEvidence(graphEvidence, edge.SkillBName, edge.SkillAName, edge.Confidence, seedScores, poolScores);
        }

        return graphEvidence
            .Select(e => (Name: e.Key, Score: SkillGraphConstants.AssociationGraphWeight * e.Value
                                              + (1 - SkillGraphConstants.AssociationGraphWeight) * poolScores[e.Key]))
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .Take(slots)
            .Select(c => c.Name)
            .ToList();
    }

    private static void AddEvidence(
        Dictionary<string, double> graphEvidence,
        string seed,
        string neighbour,
        double confidence,
        IReadOnlyDictionary<string, double> seedScores,
        IReadOnlyDictionary<string, double> poolScores)
    {
        if (!seedScores.TryGetValue(seed, out var seedScore) || !poolScores.ContainsKey(neighbour))
        {
            return;
        }

        var evidence = seedScore * confidence;
        if (!graphEvidence.TryGetValue(neighbour, out var best) || evidence > best)
        {
            graphEvidence[neighbour] = evidence;
        }
    }
}
