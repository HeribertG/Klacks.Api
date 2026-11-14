// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The in-turn tool search: the model names what it is missing ("assign a contract; add someone to a group"), and the
/// skill looks each description up in the skill index, keeps the skills the user may use that are not always-on, and
/// offers them to the turn through ITurnToolsetExtension. The chat loop adds them to the toolset after this tool round,
/// so they are callable from the next step on. The reply lists the found skills with their descriptions.
/// </summary>
/// <param name="needs">One to three short descriptions of the missing capabilities, separated by ';'</param>

using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation(ToolSearchConstants.SkillName)]
public class FindToolsSkill : BaseSkillImplementation
{
    private readonly ISkillCacheService _skillCacheService;
    private readonly IKnowledgeRetrievalService _knowledgeRetrieval;
    private readonly ITurnToolsetExtension _toolsetExtension;

    public FindToolsSkill(
        ISkillCacheService skillCacheService,
        IKnowledgeRetrievalService knowledgeRetrieval,
        ITurnToolsetExtension toolsetExtension)
    {
        _skillCacheService = skillCacheService;
        _knowledgeRetrieval = knowledgeRetrieval;
        _toolsetExtension = toolsetExtension;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var needs = (GetParameter<string>(parameters, ToolSearchConstants.NeedsParameter) ?? string.Empty)
            .Split(ToolSearchConstants.NeedSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(ToolSearchConstants.MaxNeedsPerCall)
            .ToList();
        if (needs.Count == 0)
        {
            return SkillResult.Error("Describe at least one missing capability in 'needs', e.g. \"assign a contract\".");
        }

        var agent = await _skillCacheService.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No assistant agent is configured, so no tools can be searched.");
        }

        var searchable = (await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken))
            .Where(s => !s.AlwaysOn
                        && !string.Equals(s.Name, ToolSearchConstants.SkillName, StringComparison.OrdinalIgnoreCase)
                        && Permissions.HasAllRequiredPermissions(context.UserPermissions, s.RequiredPermission))
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (searchable.Count == 0)
        {
            return SkillResult.SuccessResult(new { tools = Array.Empty<object>() }, "No further tools are available to you.");
        }

        var found = await SearchAsync(needs, searchable, context, cancellationToken);
        var accepted = _toolsetExtension.Offer(
            context.TurnId,
            found.Select(s => SkillFunctionConverter.ToFunction(s, ToolsetSkillSource.ToolSearch)).ToList());
        var tools = accepted.Select(f => new { name = f.Name, description = f.Description }).ToList();

        if (tools.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { tools },
                found.Count == 0
                    ? "No matching tools were found; do not retry with the same words."
                    : "The matching tools are already available to you or the per-answer limit is reached; use your current tools.");
        }

        return SkillResult.SuccessResult(
            new { tools },
            $"Found {tools.Count} tool(s). They are available from your next step on; call them then.");
    }

    private async Task<List<AgentSkill>> SearchAsync(
        IReadOnlyList<string> needs,
        IReadOnlyDictionary<string, AgentSkill> searchable,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        var isAdmin = context.UserPermissions.Contains(Roles.Admin);
        var found = new List<AgentSkill>();
        foreach (var need in needs)
        {
            var result = await _knowledgeRetrieval.RetrieveAsync(
                need, context.UserPermissions, isAdmin, ToolSearchConstants.HitsPerNeed, currentRoute: null,
                cancellationToken, KnowledgeEntryKind.Skill, searchable.Keys.ToList());
            foreach (var candidate in result.Candidates.Take(ToolSearchConstants.HitsPerNeed))
            {
                if (searchable.TryGetValue(candidate.Entry.SourceId, out var skill) && !found.Contains(skill))
                {
                    found.Add(skill);
                }
            }
        }

        return found;
    }
}
