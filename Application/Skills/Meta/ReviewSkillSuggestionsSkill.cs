// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that shows the admin pending auto-detected skill suggestions and allows accepting
/// or dismissing them. Accepting immediately calls create_agent_skill.
/// </summary>
/// <param name="action">Action to perform: "list", "accept", or "dismiss"</param>
/// <param name="gapId">ID of the SkillGapRecord to accept or dismiss (required for accept/dismiss)</param>

using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("review_skill_suggestions")]
public class ReviewSkillSuggestionsSkill : BaseSkillImplementation
{
    private readonly ISkillGapRepository _skillGapRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly SkillRegistryInitializer _skillRegistryInitializer;

    private const int MinOccurrences = 1;

    public ReviewSkillSuggestionsSkill(
        ISkillGapRepository skillGapRepository,
        IAgentRepository agentRepository,
        IAgentSkillRepository agentSkillRepository,
        SkillRegistryInitializer skillRegistryInitializer)
    {
        _skillGapRepository = skillGapRepository;
        _agentRepository = agentRepository;
        _agentSkillRepository = agentSkillRepository;
        _skillRegistryInitializer = skillRegistryInitializer;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var action = GetParameter<string>(parameters, "action") ?? ReviewActions.List;
        var gapId = GetParameter<string>(parameters, "gapId");

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No default agent found.");
        }

        return action.ToLowerInvariant() switch
        {
            ReviewActions.List => await ListSuggestionsAsync(agent.Id, cancellationToken),
            ReviewActions.Accept => await AcceptSuggestionAsync(agent.Id, gapId, cancellationToken),
            ReviewActions.Dismiss => await DismissSuggestionAsync(agent.Id, gapId, cancellationToken),
            _ => SkillResult.Error($"Unknown action '{action}'. Valid actions: list, accept, dismiss.")
        };
    }

    private async Task<SkillResult> ListSuggestionsAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var gaps = await _skillGapRepository.GetPendingAsync(agentId, MinOccurrences, cancellationToken);

        var suggestions = gaps.Select(g => new
        {
            Id = g.Id.ToString(),
            g.DetectedIntent,
            g.OccurrenceCount,
            g.SuggestedSkillName,
            g.SuggestedDescription,
            g.Status,
            FirstDetected = g.FirstDetectedAt.ToString("yyyy-MM-dd"),
            LastDetected = g.LastDetectedAt.ToString("yyyy-MM-dd")
        }).ToList();

        return SkillResult.SuccessResult(
            new { Suggestions = suggestions, TotalCount = suggestions.Count },
            $"Found {suggestions.Count} pending skill suggestion(s).");
    }

    private async Task<SkillResult> AcceptSuggestionAsync(
        Guid agentId,
        string? gapId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gapId) || !Guid.TryParse(gapId, out var parsedId))
        {
            return SkillResult.Error("Parameter 'gapId' is required and must be a valid GUID for action 'accept'.");
        }

        var gaps = await _skillGapRepository.GetPendingAsync(agentId, MinOccurrences, cancellationToken);
        var gap = gaps.FirstOrDefault(g => g.Id == parsedId);
        if (gap == null)
        {
            return SkillResult.Error($"Skill gap '{gapId}' not found or is not in a pending state.");
        }

        if (string.IsNullOrWhiteSpace(gap.SuggestedSkillName))
        {
            return SkillResult.Error("This suggestion does not yet have a proposed skill name. Wait for the suggestion background service to run.");
        }

        var skillName = gap.SuggestedSkillName.Trim().ToLowerInvariant().Replace(' ', '_');

        var existing = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);
        if (existing.Any(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase)))
        {
            gap.Status = SkillGapStatuses.Accepted;
            gap.UpdateTime = DateTime.UtcNow;
            await _skillGapRepository.UpdateAsync(gap, cancellationToken);
            return SkillResult.Error($"A skill named '{skillName}' already exists. Gap marked as accepted.");
        }

        var agentSkill = new AgentSkill
        {
            AgentId = agentId,
            Name = skillName,
            Description = gap.SuggestedDescription ?? gap.DetectedIntent,
            Category = "Action",
            ExecutionType = LlmExecutionTypes.UiAction,
            HandlerConfig = "{}",
            TriggerKeywords = "[]",
            IsEnabled = true,
            Version = 1
        };

        await _agentSkillRepository.AddAsync(agentSkill, cancellationToken);
        await _skillRegistryInitializer.InitializeAsync(cancellationToken);

        gap.Status = SkillGapStatuses.Accepted;
        gap.UpdateTime = DateTime.UtcNow;
        await _skillGapRepository.UpdateAsync(gap, cancellationToken);

        return SkillResult.SuccessResult(
            new { SkillName = skillName },
            $"Skill '{skillName}' created from suggestion and is now available.");
    }

    private async Task<SkillResult> DismissSuggestionAsync(
        Guid agentId,
        string? gapId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gapId) || !Guid.TryParse(gapId, out var parsedId))
        {
            return SkillResult.Error("Parameter 'gapId' is required and must be a valid GUID for action 'dismiss'.");
        }

        var gaps = await _skillGapRepository.GetPendingAsync(agentId, MinOccurrences, cancellationToken);
        var gap = gaps.FirstOrDefault(g => g.Id == parsedId);
        if (gap == null)
        {
            return SkillResult.Error($"Skill gap '{gapId}' not found or is not in a pending state.");
        }

        gap.Status = SkillGapStatuses.Dismissed;
        gap.UpdateTime = DateTime.UtcNow;
        await _skillGapRepository.UpdateAsync(gap, cancellationToken);

        return SkillResult.SuccessResult(null, $"Skill suggestion '{gap.DetectedIntent}' dismissed.");
    }

    private static class ReviewActions
    {
        public const string List = "list";
        public const string Accept = "accept";
        public const string Dismiss = "dismiss";
    }
}
