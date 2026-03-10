// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_groups")]
public class ListGroupsSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;

    public ListGroupsSkill(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var rootOnly = GetParameter<bool>(parameters, "rootOnly", false);

        var allGroups = await _groupRepository.List();
        var today = DateTime.UtcNow.Date;

        var groups = allGroups
            .Where(g => !g.IsDeleted)
            .Where(g => g.ValidFrom <= today && (g.ValidUntil == null || g.ValidUntil >= today))
            .Where(g => !rootOnly || g.Parent == null)
            .Where(g => string.IsNullOrEmpty(searchTerm) ||
                       (g.Name != null && g.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.Parent,
                g.ValidFrom,
                g.ValidUntil,
                IsRoot = g.Parent == null
            })
            .ToList();

        var resultData = new
        {
            Groups = groups,
            TotalCount = groups.Count,
            SearchTerm = searchTerm,
            RootOnly = rootOnly
        };

        var message = $"Found {groups.Count} group(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") +
                      (rootOnly ? " (root level only)" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
