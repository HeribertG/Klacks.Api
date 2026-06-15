// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Proactive "you did X — shall I do Y?" suggestion logic (the hero feature). Given a just-executed
/// skill it returns the highest-confidence active *sequential* successor that has not already been
/// suggested this session, honouring the per-session frequency cap. Active sequential edges already
/// meet the high proactivity confidence bar (the learner only promotes them at ≥ 0.8). Returns null
/// when there is nothing worth suggesting. The live emission into the proactive trigger pipeline is
/// wired by the caller (AgentTriggerService), which adds rate limiting and user preferences.
/// </summary>
/// <param name="repository">Source of the agent's skill-relationship edges.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public class SkillSequenceSuggester : ISkillSequenceSuggester
{
    private readonly ISkillRelationRepository _repository;

    public SkillSequenceSuggester(ISkillRelationRepository repository)
    {
        _repository = repository;
    }

    public async Task<string?> SuggestNextAsync(
        Guid agentId,
        string justExecutedSkill,
        IReadOnlyCollection<string> alreadySuggested,
        CancellationToken cancellationToken = default)
    {
        if (alreadySuggested.Count >= SkillGraphConstants.MaxProactiveSuggestionsPerSession)
        {
            return null;
        }

        var edges = await _repository.GetByAgentAsync(agentId, cancellationToken);
        return SelectSuccessor(edges, justExecutedSkill, alreadySuggested);
    }

    public static string? SelectSuccessor(
        IReadOnlyList<SkillRelation> edges, string justExecutedSkill, IReadOnlyCollection<string> alreadySuggested)
    {
        var excluded = new HashSet<string>(alreadySuggested, StringComparer.OrdinalIgnoreCase);
        return edges
            .Where(e => e.Type == SkillRelationType.Sequential
                && e.Status == SkillRelationStatus.Active
                && string.Equals(e.SkillAName, justExecutedSkill, StringComparison.OrdinalIgnoreCase)
                && !excluded.Contains(e.SkillBName))
            .OrderByDescending(e => e.Confidence)
            .ThenBy(e => e.SkillBName, StringComparer.Ordinal)
            .Select(e => e.SkillBName)
            .FirstOrDefault();
    }
}
