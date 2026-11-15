// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// See IToolSearchToolsetAppender. find_tools needs no permission; it is added only when the switch is on, the skill
/// is enabled for the agent and the toolset does not hold it yet.
/// </summary>
/// <param name="skillCacheService">The agent's enabled skills</param>
/// <param name="options">The tool-search switch</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.ToolSearch;

public sealed class ToolSearchToolsetAppender : IToolSearchToolsetAppender
{
    private readonly ISkillCacheService _skillCacheService;
    private readonly ToolSearchOptions _options;

    public ToolSearchToolsetAppender(ISkillCacheService skillCacheService, ToolSearchOptions options)
    {
        _skillCacheService = skillCacheService;
        _options = options;
    }

    public async Task AppendAsync(Agent agent, SkillToolsetResult toolset, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || toolset.Functions.Any(f =>
                string.Equals(f.Name, ToolSearchConstants.SkillName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var findTools = skills.FirstOrDefault(s =>
            string.Equals(s.Name, ToolSearchConstants.SkillName, StringComparison.OrdinalIgnoreCase));
        if (findTools != null)
        {
            toolset.Functions.Add(SkillFunctionConverter.ToFunction(findTools, ToolsetSkillSource.ToolSearch));
        }
    }
}
