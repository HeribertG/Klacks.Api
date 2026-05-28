// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that navigates to the schedule (Dienstplan / shift planner) page, optionally
/// pre-filtered to a specific group. When the user just edited an employee that was assigned
/// to a group, pass that group's name so the schedule opens with the correct group filter.
/// </summary>
/// <param name="groupSearchRepository">Resolves group name to id via the same search index used elsewhere</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("open_schedule")]
public class OpenScheduleSkill : BaseSkillImplementation
{
    private const string ScheduleRoute = "/workplace/schedule";
    private const string GroupIdQueryParam = "groupId";
    private const int GroupSearchLimit = 10;

    private readonly IGroupSearchRepository _groupSearchRepository;

    public OpenScheduleSkill(IGroupSearchRepository groupSearchRepository)
    {
        _groupSearchRepository = groupSearchRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetParameter<string>(parameters, "groupName");

        if (string.IsNullOrWhiteSpace(groupName))
        {
            return SkillResult.Navigation(
                new { Route = ScheduleRoute, GroupId = (Guid?)null, GroupName = (string?)null },
                "Open schedule");
        }

        var result = await _groupSearchRepository.SearchAsync(
            searchTerm: groupName,
            limit: GroupSearchLimit,
            cancellationToken: cancellationToken);

        if (result.TotalCount == 0)
        {
            return SkillResult.Navigation(
                new { Route = ScheduleRoute, GroupId = (Guid?)null, GroupName = (string?)null },
                $"No group found matching '{groupName}'. Opening schedule without group filter.");
        }

        if (result.TotalCount > 1)
        {
            var matches = result.Items
                .Select(g => new { g.Id, g.Name })
                .ToList();

            return new SkillResult
            {
                Success = true,
                Data = new { EntityType = "group", Matches = matches, Count = matches.Count },
                Message = $"Multiple groups match '{groupName}'. Please specify which one to open the schedule for.",
                Type = SkillResultType.Data
            };
        }

        var group = result.Items.First();
        var route = $"{ScheduleRoute}?{GroupIdQueryParam}={group.Id}";

        return SkillResult.Navigation(
            new { Route = route, GroupId = group.Id, GroupName = group.Name },
            $"Open schedule for group '{group.Name}'");
    }
}
