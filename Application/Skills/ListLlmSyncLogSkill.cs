// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the history of model catalogue syncs from the repository: which provider was checked when,
/// how many models appeared, disappeared or failed their probe. Read-only skill, same pattern as
/// list_scenarios.
/// </summary>
/// <param name="limit">Maximum number of entries returned, newest first.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_llm_sync_log")]
public class ListLlmSyncLogSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 20;

    private readonly ILLMRepository _llmRepository;

    public ListLlmSyncLogSkill(ILLMRepository llmRepository)
    {
        _llmRepository = llmRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _llmRepository.GetSyncNotificationsHistoryAsync();

        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var projected = notifications
            .OrderByDescending(n => n.SyncedAt)
            .Take(limit)
            .Select(n => new
            {
                n.Id,
                n.ProviderId,
                n.ProviderName,
                n.NewModelsCount,
                n.DeactivatedModelsCount,
                n.FailedModelsCount,
                n.NewModelNames,
                n.DeactivatedModelNames,
                n.SyncedAt
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, TotalAvailable = notifications.Count, SyncLog = projected },
            $"Found {projected.Count} sync entr(ies) of {notifications.Count} in total.");
    }
}
