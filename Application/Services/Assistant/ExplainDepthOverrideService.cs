// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persists a user's explanation-depth override as their own memory row: beginner or advanced writes a new row first
/// and only then retires the older override rows, so a crash in between still resolves to the newest choice; auto
/// retires every override row so the usage heuristic decides again. The cached depth is dropped after each change.
/// </summary>
/// <param name="memoryRepository">Stores and retires the user's override memory rows</param>
/// <param name="agentRepository">Resolves the single configured agent the memory belongs to</param>
/// <param name="explainDepthResolver">Drops the user's cached explanation depth after the change</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class ExplainDepthOverrideService : IExplainDepthOverrideService
{
    private static readonly HashSet<string> LevelValues = new(StringComparer.Ordinal)
    {
        ExplainDepthDefaults.OverrideValueBeginner,
        ExplainDepthDefaults.OverrideValueAdvanced
    };

    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IExplainDepthResolver _explainDepthResolver;

    public ExplainDepthOverrideService(
        IAgentMemoryRepository memoryRepository,
        IAgentRepository agentRepository,
        IExplainDepthResolver explainDepthResolver)
    {
        _memoryRepository = memoryRepository;
        _agentRepository = agentRepository;
        _explainDepthResolver = explainDepthResolver;
    }

    public async Task<ExplainDepthOverrideOutcome> SetAsync(Guid userId, string? depth, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return ExplainDepthOverrideOutcome.NoUser;
        }

        var normalized = depth?.Trim().ToLowerInvariant();

        if (string.Equals(normalized, ExplainDepthDefaults.OverrideValueAuto, StringComparison.Ordinal))
        {
            await _memoryRepository.SoftDeleteByUserAndKeyAsync(
                userId, ExplainDepthDefaults.OverrideMemoryKey, cancellationToken: cancellationToken);
            _explainDepthResolver.Invalidate(userId);
            return ExplainDepthOverrideOutcome.Cleared;
        }

        if (normalized == null || !LevelValues.Contains(normalized))
        {
            return ExplainDepthOverrideOutcome.UnknownDepth;
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return ExplainDepthOverrideOutcome.NoAgent;
        }

        var memory = new AgentMemory
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            UserId = userId,
            Key = ExplainDepthDefaults.OverrideMemoryKey,
            Content = normalized,
            Category = MemoryCategories.UserPreference,
            Importance = ExplainDepthDefaults.OverrideImportance,
            IsPinned = false,
            Source = MemorySources.Chat
        };

        await _memoryRepository.AddAsync(memory, cancellationToken);
        await _memoryRepository.SoftDeleteByUserAndKeyAsync(
            userId, ExplainDepthDefaults.OverrideMemoryKey, memory.Id, cancellationToken);
        _explainDepthResolver.Invalidate(userId);

        return ExplainDepthOverrideOutcome.Stored;
    }
}
