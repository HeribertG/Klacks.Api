// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that lists AnalyseScenarios — optionally filtered by group. Used after AutoWizard runs so Klacksy
/// can show the user the proposed scenarios and offer to accept or reject them. Without a group it lists the
/// scenarios of every group the caller may see; a caller with a restricted group scope sees only scenarios
/// of groups inside that scope (group-less scenarios span all groups and are therefore left out for them).
/// By default only open (Active) scenarios are returned so already accepted/rejected ones do not show as
/// pending proposals; pass onlyOpen=false to include the full history. All filters run in the database. A
/// groupId outside the caller's scope is answered exactly like a missing group (same text, no name), never with an
/// empty list, so the skill reveals neither the existence nor the name of a hidden group. The scope comes
/// from IGroupVisibilityGuard - the same decision as the AnalyseScenarios HTTP handlers - so a non-admin without any
/// visibility row sees no scenarios here either (fail-closed), while a background call without a user is unrestricted.
/// </summary>
/// <param name="groupId">Optional group UUID; if omitted, lists scenarios across all visible groups.</param>
/// <param name="onlyOpen">Optional; defaults to true. When true only Active (open) scenarios are returned.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_scenarios")]
public class ListScenariosSkill : BaseSkillImplementation
{
    private const string GroupIdParameter = "groupId";
    private const string OnlyOpenParameter = "onlyOpen";
    private const string GroupNotFoundFormat = "Group with ID {0} not found.";

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public ListScenariosSkill(
        IAnalyseScenarioRepository scenarioRepository,
        IGroupRepository groupRepository,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _scenarioRepository = scenarioRepository;
        _groupRepository = groupRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        Guid? groupId = null;
        var groupIdRaw = GetParameter<string>(parameters, GroupIdParameter);
        if (!string.IsNullOrWhiteSpace(groupIdRaw))
        {
            if (!Guid.TryParse(groupIdRaw, out var parsed))
            {
                return SkillResult.Error($"Invalid groupId format: '{groupIdRaw}'. Expected UUID.");
            }

            groupId = parsed;
        }

        var onlyOpenRaw = GetParameter<string>(parameters, OnlyOpenParameter);
        var onlyOpen = string.IsNullOrWhiteSpace(onlyOpenRaw)
            || !bool.TryParse(onlyOpenRaw, out var parsedOpen)
            || parsedOpen;

        var scope = await _groupVisibilityGuard.GetScopeAsync(cancellationToken);

        string? groupName = null;
        if (groupId.HasValue)
        {
            var group = await _groupRepository.Get(groupId.Value);
            if (group == null
                || group.IsDeleted
                || !await _groupVisibilityGuard.IsGroupVisibleAsync(group.Id, cancellationToken))
            {
                return SkillResult.Error(string.Format(GroupNotFoundFormat, groupId));
            }

            groupName = group.Name;
        }

        var scenarios = await _scenarioRepository.ListVisibleAsync(
            groupId,
            onlyOpen,
            scope.IsUnrestricted ? null : scope.VisibleRootIds,
            cancellationToken);

        var projected = scenarios
            .Select(s => new
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Token = s.Token,
                Status = s.Status.ToString(),
                CreateTime = s.CreateTime,
                GroupId = s.GroupId,
                GroupName = s.Group?.Name,
                FromDate = s.FromDate,
                UntilDate = s.UntilDate
            })
            .ToList();

        var label = groupId.HasValue ? $"group '{groupName}'" : "all visible groups";
        var filterNote = onlyOpen ? " open" : string.Empty;
        return SkillResult.SuccessResult(
            new { Count = projected.Count, OnlyOpen = onlyOpen, Scenarios = projected },
            $"Found {projected.Count}{filterNote} analyse scenario(s) for {label}.");
    }
}
