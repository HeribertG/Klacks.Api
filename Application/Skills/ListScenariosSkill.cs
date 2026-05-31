// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that lists AnalyseScenarios — optionally filtered by group. Used after AutoWizard runs so Klacksy
/// can show the user the proposed scenarios and offer accept_scenario / reject_scenario actions. By
/// default only open (Active) scenarios are returned so already accepted/rejected ones do not show as
/// pending proposals; pass onlyOpen=false to include the full history.
/// </summary>
/// <param name="groupId">Optional group UUID; if omitted, lists scenarios across all groups.</param>
/// <param name="onlyOpen">Optional; defaults to true. When true only Active (open) scenarios are returned.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_scenarios")]
public class ListScenariosSkill : BaseSkillImplementation
{
    private readonly IAnalyseScenarioRepository _scenarioRepository;

    public ListScenariosSkill(IAnalyseScenarioRepository scenarioRepository)
    {
        _scenarioRepository = scenarioRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        Guid? groupId = null;
        var groupIdRaw = GetParameter<string>(parameters, "groupId");
        if (!string.IsNullOrWhiteSpace(groupIdRaw) && Guid.TryParse(groupIdRaw, out var parsed))
        {
            groupId = parsed;
        }

        var onlyOpenRaw = GetParameter<string>(parameters, "onlyOpen");
        var onlyOpen = string.IsNullOrWhiteSpace(onlyOpenRaw)
            || !bool.TryParse(onlyOpenRaw, out var parsedOpen)
            || parsedOpen;

        var scenarios = await _scenarioRepository.GetByGroupAsync(groupId, cancellationToken);
        if (onlyOpen)
        {
            scenarios = scenarios
                .Where(s => s.Status == AnalyseScenarioStatus.Active)
                .ToList();
        }

        var projected = scenarios
            .Select(s => new
            {
                Id = s.Id,
                Name = s.Name,
                Token = s.Token,
                Status = s.Status.ToString(),
                CreateTime = s.CreateTime,
                GroupId = s.GroupId
            })
            .ToList();

        var label = groupId.HasValue ? $"group {groupId}" : "all groups";
        var filterNote = onlyOpen ? " open" : string.Empty;
        return SkillResult.SuccessResult(
            new { Count = projected.Count, OnlyOpen = onlyOpen, Scenarios = projected },
            $"Found {projected.Count}{filterNote} analyse scenario(s) for {label}.");
    }
}
