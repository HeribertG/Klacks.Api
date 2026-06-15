// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Derives the grounded substrate prior of the emergent skill-relationship graph: two skills that
/// operate on the same domain entity get a co-required prior edge. Runs at startup after skills are
/// seeded; deterministic, idempotent, and never touches learned edges. This is the Bayesian prior
/// that the later experience-based learning corrects up or down.
/// </summary>
/// <param name="agentSkillRepository">Source of the enabled skills per agent.</param>
/// <param name="skillRelationRepository">Persistence of the derived edges.</param>
/// <param name="logger">Diagnostic logging of how many edges were added.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public class SubstratePriorDeriver : ISubstratePriorDeriver
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillRelationRepository _skillRelationRepository;
    private readonly ILogger<SubstratePriorDeriver> _logger;

    public SubstratePriorDeriver(
        IAgentSkillRepository agentSkillRepository,
        ISkillRelationRepository skillRelationRepository,
        ILogger<SubstratePriorDeriver> logger)
    {
        _agentSkillRepository = agentSkillRepository;
        _skillRelationRepository = skillRelationRepository;
        _logger = logger;
    }

    public async Task DeriveAsync(CancellationToken cancellationToken = default)
    {
        var skills = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);

        var totalAdded = 0;
        foreach (var agentSkills in skills.GroupBy(s => s.AgentId))
        {
            totalAdded += await DeriveForAgentAsync(agentSkills.Key, agentSkills.ToList(), cancellationToken);
        }

        _logger.LogInformation(
            "Substrate prior derivation complete: {Added} new co-required edge(s) added.", totalAdded);
    }

    private async Task<int> DeriveForAgentAsync(
        Guid agentId, List<AgentSkill> agentSkills, CancellationToken cancellationToken)
    {
        var pairs = BuildCoRequiredPairs(agentSkills);
        if (pairs.Count == 0)
        {
            return 0;
        }

        var existing = await _skillRelationRepository.GetByAgentAsync(agentId, cancellationToken);
        var existingKeys = existing
            .Where(r => r.Type == SkillRelationType.CoRequired)
            .Select(r => (r.SkillAName, r.SkillBName))
            .ToHashSet();

        var toAdd = pairs
            .Where(pair => !existingKeys.Contains(pair))
            .Select(pair => NewDerivedEdge(agentId, pair.SkillAName, pair.SkillBName))
            .ToList();

        if (toAdd.Count > 0)
        {
            await _skillRelationRepository.AddRangeAsync(toAdd, cancellationToken);
        }

        return toAdd.Count;
    }

    private static SkillRelation NewDerivedEdge(Guid agentId, string skillAName, string skillBName)
    {
        return new SkillRelation
        {
            AgentId = agentId,
            SkillAName = skillAName,
            SkillBName = skillBName,
            Type = SkillRelationType.CoRequired,
            Confidence = SkillGraphConstants.SubstratePriorConfidence,
            SupportCount = 0,
            ContradictionCount = 0,
            Provenance = SkillGraphConstants.SubstratePriorProvenance,
            Source = SkillRelationSource.Derived,
            Status = SkillRelationStatus.Candidate,
        };
    }

    private static HashSet<(string SkillAName, string SkillBName)> BuildCoRequiredPairs(List<AgentSkill> agentSkills)
    {
        var entityToSkills = new Dictionary<string, List<string>>();
        foreach (var skill in agentSkills)
        {
            if (!SkillEntityMap.Map.TryGetValue(skill.Name, out var entities))
            {
                continue;
            }

            foreach (var entity in entities)
            {
                if (!entityToSkills.TryGetValue(entity, out var list))
                {
                    list = new List<string>();
                    entityToSkills[entity] = list;
                }

                list.Add(skill.Name);
            }
        }

        var pairs = new HashSet<(string, string)>();
        foreach (var skillNames in entityToSkills.Values)
        {
            var distinct = skillNames.Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();
            for (var i = 0; i < distinct.Count; i++)
            {
                for (var j = i + 1; j < distinct.Count; j++)
                {
                    pairs.Add((distinct[i], distinct[j]));
                }
            }
        }

        return pairs;
    }
}
