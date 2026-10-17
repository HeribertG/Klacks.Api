// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that lets the signed-in user choose how deep Klacksy explains from now on: beginner or advanced is stored as the
/// user's own explanation-depth override memory (newest row wins, older rows are retired afterwards), auto removes the
/// override so the usage heuristic decides again. The user's cached depth is dropped so the next turn uses the choice.
/// </summary>
/// <param name="memoryRepository">Stores and retires the user's override memory rows</param>
/// <param name="agentRepository">Resolves the single configured agent the memory belongs to</param>
/// <param name="explainDepthResolver">Drops the user's cached explanation depth after the change</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(ExplainDepthDefaults.SetSkillName)]
public class SetExplainDepthSkill : BaseSkillImplementation
{
    private const string AllowedValuesText = "beginner, advanced or auto";

    private static readonly HashSet<string> LevelValues = new(StringComparer.Ordinal)
    {
        ExplainDepthDefaults.OverrideValueBeginner,
        ExplainDepthDefaults.OverrideValueAdvanced
    };

    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IExplainDepthResolver _explainDepthResolver;

    public SetExplainDepthSkill(
        IAgentMemoryRepository memoryRepository,
        IAgentRepository agentRepository,
        IExplainDepthResolver explainDepthResolver)
    {
        _memoryRepository = memoryRepository;
        _agentRepository = agentRepository;
        _explainDepthResolver = explainDepthResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (context.UserId == Guid.Empty)
        {
            return SkillResult.Error("The explanation depth can only be set for a signed-in user.");
        }

        var depth = GetParameter<string>(parameters, ExplainDepthDefaults.SetSkillParameterName)?.Trim().ToLowerInvariant();

        if (string.Equals(depth, ExplainDepthDefaults.OverrideValueAuto, StringComparison.Ordinal))
        {
            await _memoryRepository.SoftDeleteByUserAndKeyAsync(
                context.UserId, ExplainDepthDefaults.OverrideMemoryKey, cancellationToken: cancellationToken);
            _explainDepthResolver.Invalidate(context.UserId);

            return SkillResult.SuccessResult(
                new { Depth = depth },
                "The explanation depth is automatic again: it follows how experienced the user is. " +
                "Confirm this to the user in one short sentence.");
        }

        if (depth == null || !LevelValues.Contains(depth))
        {
            return SkillResult.Error($"Unknown explanation depth '{depth}'. Use {AllowedValuesText}.");
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No agent is configured yet.");
        }

        var memory = new AgentMemory
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            UserId = context.UserId,
            Key = ExplainDepthDefaults.OverrideMemoryKey,
            Content = depth,
            Category = MemoryCategories.UserPreference,
            Importance = ExplainDepthDefaults.OverrideImportance,
            IsPinned = false,
            Source = MemorySources.Chat
        };

        await _memoryRepository.AddAsync(memory, cancellationToken);
        await _memoryRepository.SoftDeleteByUserAndKeyAsync(
            context.UserId, ExplainDepthDefaults.OverrideMemoryKey, memory.Id, cancellationToken);
        _explainDepthResolver.Invalidate(context.UserId);

        return SkillResult.SuccessResult(
            new { Depth = depth },
            $"The explanation depth is now '{depth}' for this user and applies from the next answer on until the user " +
            "changes it. Confirm this to the user in one short sentence.");
    }
}
