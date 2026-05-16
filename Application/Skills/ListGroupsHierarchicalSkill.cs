// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the group tree starting from rootId (or all roots if omitted) with depth-level
/// information, so the LLM can reason about parent / child relationships instead of a flat list.
/// </summary>
/// <param name="rootId">Optional. UUID of the subtree root; all roots if omitted.</param>
/// <param name="maxDepth">Optional. Max depth from root (1-based); unlimited if omitted.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_groups_hierarchical")]
public class ListGroupsHierarchicalSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;

    public ListGroupsHierarchicalSkill(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rootIdStr = GetParameter<string>(parameters, "rootId");
        var maxDepth = GetParameter<int?>(parameters, "maxDepth");

        Guid? rootId = null;
        if (!string.IsNullOrWhiteSpace(rootIdStr))
        {
            if (!Guid.TryParse(rootIdStr, out var parsedRoot))
            {
                return SkillResult.Error($"Invalid rootId UUID: {rootIdStr}");
            }
            rootId = parsedRoot;
        }

        var tree = (await _groupRepository.GetTree(rootId)).ToList();

        var byId = tree.ToDictionary(g => g.Id);
        var rows = tree
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.Parent,
                ParentName = g.Parent.HasValue && byId.TryGetValue(g.Parent.Value, out var p) ? p.Name : null,
                Depth = ComputeDepth(g, byId),
                g.Lft,
                g.Rgt,
                g.ValidFrom,
                g.ValidUntil
            })
            .Where(r => !maxDepth.HasValue || r.Depth <= maxDepth.Value)
            .OrderBy(r => r.Lft)
            .ToList();

        return SkillResult.SuccessResult(
            new
            {
                Groups = rows,
                TotalCount = rows.Count,
                RootId = rootId,
                MaxDepth = maxDepth
            },
            $"Returned {rows.Count} group(s) in hierarchical order" +
            (rootId.HasValue ? $" rooted at {rootId.Value}." : "."));
    }

    private static int ComputeDepth(Group group, IReadOnlyDictionary<Guid, Group> byId)
    {
        var depth = 0;
        var current = group;
        while (current.Parent.HasValue && byId.TryGetValue(current.Parent.Value, out var parent))
        {
            depth++;
            current = parent;
            if (depth > 32) break;
        }
        return depth;
    }
}
