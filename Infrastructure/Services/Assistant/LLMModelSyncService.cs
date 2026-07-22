// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Assistant;

/// <summary>
/// Synchronizes LLM models by querying each enabled provider's discovery API,
/// creating new models, restoring ones that reappear, and soft-deleting ones
/// no longer offered by the provider.
/// </summary>

public partial class LLMModelSyncService : ILLMModelSyncService
{
    private readonly ILLMRepository _repository;
    private readonly ILLMProviderFactory _factory;
    private readonly ILogger<LLMModelSyncService> _logger;

    public LLMModelSyncService(
        ILLMRepository repository,
        ILLMProviderFactory factory,
        ILogger<LLMModelSyncService> logger)
    {
        _repository = repository;
        _factory = factory;
        _logger = logger;
    }

    public async Task SyncAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providers = await _factory.GetEnabledProvidersAsync();

        foreach (var provider in providers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await SyncProviderAsync(provider, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLMModelSyncService - {Provider} sync failed: {Error}",
                    provider.ProviderName, ex.Message);
            }
        }
    }

    private async Task SyncProviderAsync(
        ILLMProvider provider,
        CancellationToken cancellationToken)
    {
        var discovered = await provider.GetAvailableModelsAsync();

        if (discovered is null)
            return;

        var providerModels = await _repository.GetModelsByProviderIncludingDeletedAsync(provider.ProviderId);

        var newNames = new List<string>();
        var deactivatedNames = new List<string>();
        var failedCount = 0;
        var modelTestResults = new List<LLMModelTestResult>();

        foreach (var apiModel in discovered)
        {
            var existing = providerModels.FirstOrDefault(m =>
                string.Equals(m.ApiModelId, apiModel.ApiModelId, StringComparison.OrdinalIgnoreCase));

            if (existing is not null && !existing.IsDeleted)
            {
                continue;
            }

            var isNonChatModel = NonChatModelPatterns.IsLikelyNonChatModel(apiModel.ApiModelId);
            LLMModelTestResult testResult;

            if (isNonChatModel)
            {
                testResult = new LLMModelTestResult(
                    apiModel.ApiModelId, apiModel.ModelName, false, NonChatModelPatterns.SkipReason, 0);

                _logger.LogDebug(
                    "LLMModelSyncService - {Provider}: {ModelId} skipped, non-chat model id pattern, not tested",
                    provider.ProviderName, apiModel.ApiModelId);
            }
            else
            {
                testResult = await provider.TestModelAsync(apiModel.ApiModelId);
            }

            var resultWithName = testResult with { ModelName = apiModel.ModelName };

            if (existing is null)
            {
                var newModel = new LLMModel
                {
                    Id = Guid.NewGuid(),
                    ModelId = GenerateModelId(apiModel.ApiModelId),
                    ModelName = apiModel.ModelName,
                    ApiModelId = apiModel.ApiModelId,
                    ProviderId = provider.ProviderId,
                    IsEnabled = testResult.Passed,
                    IsDeleted = !testResult.Passed,
                    DeletedTime = testResult.Passed ? null : DateTime.UtcNow,
                    IsDefault = false,
                    MaxTokens = 4096,
                    ContextWindow = 128000,
                    CostPerInputToken = 0,
                    CostPerOutputToken = 0,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow,
                };

                await _repository.CreateModelAsync(newModel);

                _logger.LogInformation(
                    "LLMModelSyncService - {Provider}: {Outcome} {ModelId} (probe {Result} in {Ms}ms)",
                    provider.ProviderName, testResult.Passed ? "inserted" : "inserted as DELETED", apiModel.ApiModelId,
                    testResult.Passed ? "passed" : (isNonChatModel ? "skipped (non-chat model)" : "failed"), testResult.DurationMs);
            }
            else if (testResult.Passed)
            {
                existing.ModelName = apiModel.ModelName;
                existing.IsEnabled = true;
                existing.IsDeleted = false;
                existing.DeletedTime = null;
                existing.UpdateTime = DateTime.UtcNow;

                await _repository.UpdateModelAsync(existing);

                _logger.LogInformation(
                    "LLMModelSyncService - {Provider}: restored, reappeared in provider list {ModelId} (test passed in {Ms}ms)",
                    provider.ProviderName, apiModel.ApiModelId, testResult.DurationMs);
            }
            else
            {
                // Already soft-deleted and the retest still fails: no state change. Skipping here
                // avoids re-stamping DeletedTime and emitting a duplicate sync notification on every
                // cycle for models the provider keeps advertising even after deprecation
                // (e.g. Gemini still lists retired models in its discovery API).
                _logger.LogDebug(
                    "LLMModelSyncService - {Provider}: {ModelId} stays deleted, still fails test, no change",
                    provider.ProviderName, apiModel.ApiModelId);
                continue;
            }

            modelTestResults.Add(resultWithName);
            newNames.Add(apiModel.ModelName);

            if (!testResult.Passed)
            {
                failedCount++;
            }
        }

        if (discovered.Count == 0)
        {
            _logger.LogWarning(
                "LLMModelSyncService - {Provider}: discovery returned an empty model list, skipping removal check",
                provider.ProviderName);
        }
        else
        {
            var discoveredIds = discovered
                .Select(m => m.ApiModelId.ToLowerInvariant())
                .ToHashSet();

            foreach (var existing in providerModels.Where(m => !m.IsDeleted))
            {
                if (discoveredIds.Contains(existing.ApiModelId.ToLowerInvariant()))
                {
                    continue;
                }

                if (existing.IsDefault)
                {
                    _logger.LogWarning(
                        "LLMModelSyncService - {Provider}: {ModelId} is the default model and no longer offered, skipping removal",
                        provider.ProviderName, existing.ApiModelId);
                    continue;
                }

                existing.IsDeleted = true;
                existing.DeletedTime = DateTime.UtcNow;
                existing.IsEnabled = false;
                existing.UpdateTime = DateTime.UtcNow;
                await _repository.UpdateModelAsync(existing);
                deactivatedNames.Add(existing.ModelName);

                _logger.LogInformation("LLMModelSyncService - {Provider}: removed model {ModelId}, no longer offered by provider",
                    provider.ProviderName, existing.ApiModelId);
            }
        }

        if (newNames.Count == 0 && deactivatedNames.Count == 0)
        {
            _logger.LogInformation("LLMModelSyncService - {Provider} sync: no changes", provider.ProviderName);
            return;
        }

        _logger.LogInformation(
            "LLMModelSyncService - {Provider} sync: {New} new/restored ({Failed} disabled), {Removed} removed",
            provider.ProviderName, newNames.Count, failedCount, deactivatedNames.Count);

        await _repository.CreateSyncNotificationAsync(new LLMSyncNotification
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.ProviderId,
            ProviderName = provider.ProviderName,
            NewModelsCount = newNames.Count,
            FailedModelsCount = failedCount,
            DeactivatedModelsCount = deactivatedNames.Count,
            NewModelNames = newNames,
            DeactivatedModelNames = deactivatedNames,
            ModelTestResults = modelTestResults,
            SyncedAt = DateTime.UtcNow,
            IsRead = false,
            CreateTime = DateTime.UtcNow,
            UpdateTime = DateTime.UtcNow,
        });
    }

    private static string GenerateModelId(string apiModelId)
    {
        var slug = apiModelId.ToLowerInvariant()
            .Replace('.', '-')
            .Replace('/', '-')
            .Replace('_', '-');
        return CollapseMultipleDashes().Replace(slug, "-").Trim('-');
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseMultipleDashes();
}
