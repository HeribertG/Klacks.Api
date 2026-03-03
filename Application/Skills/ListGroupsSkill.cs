// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ListGroupsSkill : BaseSkill
{
    private readonly IGroupRepository _groupRepository;

    public override string Name => "list_groups";

    public override string Description =>
        "Lists all available groups (teams, departments, organizational units) in the system. " +
        "Returns group names and IDs. Use this to find group IDs before adding clients to groups.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewGroups" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "searchTerm",
            "Optional search term to filter groups by name",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "rootOnly",
            "If true, only return root-level groups (no parent)",
            SkillParameterType.Boolean,
            Required: false,
            DefaultValue: "false")
    };

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
