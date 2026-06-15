// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The proactive "hero": after a skill executes, if a high-confidence active sequential successor
/// exists it raises a suggestion event into the existing proactive trigger pipeline (which applies
/// per-user preference and the daily rate-limit frequency cap, then pushes the message). Skill names
/// are rendered as their human-readable descriptions, never the internal identifiers.
/// </summary>
/// <param name="relationRepository">Source of the learned skill-relationship edges.</param>
/// <param name="skillRepository">Resolves human-readable skill labels.</param>
/// <param name="triggerService">The proactive dispatch pipeline (preference + rate-limit + push).</param>

using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public class SkillSequenceProactiveNotifier : ISkillSequenceProactiveNotifier
{
    private readonly ISkillRelationRepository _relationRepository;
    private readonly IAgentSkillRepository _skillRepository;
    private readonly IAgentTriggerService _triggerService;

    public SkillSequenceProactiveNotifier(
        ISkillRelationRepository relationRepository,
        IAgentSkillRepository skillRepository,
        IAgentTriggerService triggerService)
    {
        _relationRepository = relationRepository;
        _skillRepository = skillRepository;
        _triggerService = triggerService;
    }

    public async Task NotifyAfterSkillAsync(string justExecutedSkill, CancellationToken cancellationToken = default)
    {
        var edges = await _relationRepository.GetAllAsync(cancellationToken);
        var successor = SkillSequenceSuggester.SelectSuccessor(edges, justExecutedSkill, Array.Empty<string>());
        if (successor == null)
        {
            return;
        }

        var skills = await _skillRepository.GetAllEnabledAsync(cancellationToken);
        var labels = skills
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Description, StringComparer.OrdinalIgnoreCase);

        var fromLabel = Label(labels, justExecutedSkill);
        var toLabel = Label(labels, successor);

        await _triggerService.OnEventAsync(
            new SkillSequenceSuggestionTriggerEvent(fromLabel, toLabel), cancellationToken);
    }

    private static string Label(IReadOnlyDictionary<string, string> labels, string name)
        => labels.TryGetValue(name, out var description) && !string.IsNullOrWhiteSpace(description)
            ? description
            : name;
}
