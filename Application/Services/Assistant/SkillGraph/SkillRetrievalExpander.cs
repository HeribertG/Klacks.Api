// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Silent retrieval expansion: after the semantic skill selection, pulls in high-confidence active
/// co-required neighbours of the already-selected skills to improve recall on multi-step tasks. It
/// only fills FREE tool-budget slots (never evicts a selected skill), is capped, and skips with a
/// small exploration probability to keep an unbiased control stream for the learner. Best-effort.
/// </summary>
/// <param name="repository">Source of the skill-relationship edges for the agent.</param>
/// <param name="logger">Diagnostic logging.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public class SkillRetrievalExpander : ISkillRetrievalExpander
{
    private readonly ISkillRelationRepository _repository;
    private readonly ILogger<SkillRetrievalExpander> _logger;

    public SkillRetrievalExpander(ISkillRelationRepository repository, ILogger<SkillRetrievalExpander> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentSkill>> ExpandAsync(
        Guid agentId,
        IReadOnlyList<AgentSkill> selectedSkills,
        IReadOnlyList<AgentSkill> permittedSkills,
        int freeBudget,
        CancellationToken cancellationToken = default)
    {
        if (freeBudget <= 0)
        {
            return Array.Empty<AgentSkill>();
        }

        if (Random.Shared.NextDouble() < SkillGraphConstants.ExpansionExplorationEpsilon)
        {
            return Array.Empty<AgentSkill>();
        }

        var edges = await _repository.GetByAgentAsync(agentId, cancellationToken);
        var slots = Math.Min(freeBudget, SkillGraphConstants.MaxExpansionSlots);
        return BuildExpansion(edges, selectedSkills, permittedSkills, slots);
    }

    public static IReadOnlyList<AgentSkill> BuildExpansion(
        IReadOnlyList<SkillRelation> edges,
        IReadOnlyList<AgentSkill> selectedSkills,
        IReadOnlyList<AgentSkill> permittedSkills,
        int slots)
    {
        if (slots <= 0)
        {
            return Array.Empty<AgentSkill>();
        }

        var selectedNames = selectedSkills.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permittedByName = new Dictionary<string, AgentSkill>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in permittedSkills)
        {
            permittedByName[skill.Name] = skill;
        }

        var candidates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            if (edge.Type != SkillRelationType.CoRequired || edge.Status != SkillRelationStatus.Active)
            {
                continue;
            }

            var neighbour = NeighbourOfSelected(edge, selectedNames);
            if (neighbour == null || !permittedByName.ContainsKey(neighbour))
            {
                continue;
            }

            if (!candidates.TryGetValue(neighbour, out var best) || edge.Confidence > best)
            {
                candidates[neighbour] = edge.Confidence;
            }
        }

        return candidates
            .OrderByDescending(c => c.Value)
            .ThenBy(c => c.Key, StringComparer.Ordinal)
            .Take(slots)
            .Select(c => permittedByName[c.Key])
            .ToList();
    }

    private static string? NeighbourOfSelected(SkillRelation edge, HashSet<string> selectedNames)
    {
        var aSelected = selectedNames.Contains(edge.SkillAName);
        var bSelected = selectedNames.Contains(edge.SkillBName);
        if (aSelected && !bSelected)
        {
            return edge.SkillBName;
        }

        if (bSelected && !aSelected)
        {
            return edge.SkillAName;
        }

        return null;
    }
}
