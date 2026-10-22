// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

        var changes = new ProviderSyncChanges();

        foreach (var apiModel in discovered)
        {
            await SyncDiscoveredModelAsync(provider, apiModel, providerModels, changes);
        }

        await RemoveVanishedModelsAsync(provider, discovered, providerModels, changes.DeactivatedNames);

        if (changes.NewNames.Count == 0 && changes.DeactivatedNames.Count == 0)
        {
            _logger.LogInformation("LLMModelSyncService - {Provider} sync: no changes", provider.ProviderName);
            return;
        }

        _logger.LogInformation(
            "LLMModelSyncService - {Provider} sync: {New} new/restored ({Failed} disabled), {Removed} removed",
            provider.ProviderName, changes.NewNames.Count, changes.FailedCount, changes.DeactivatedNames.Count);

        await _repository.CreateSyncNotificationAsync(BuildNotification(provider, changes));
    }

    /// <summary>
    /// Handles one model of the discovery list: an active known model is left alone, a new one is probed and
    /// inserted (deleted when the probe fails), a soft-deleted one is restored when its probe passes and otherwise
    /// left unchanged. Only inserted and restored models are recorded in the changes.
    /// </summary>
    /// <param name="provider">Provider that advertised the model.</param>
    /// <param name="apiModel">The advertised model.</param>
    /// <param name="providerModels">The stored models of the provider, deleted ones included.</param>
    /// <param name="changes">Collects the inserted and restored models of this sync.</param>
    private async Task SyncDiscoveredModelAsync(
        ILLMProvider provider,
        LLMModelDiscovery apiModel,
        List<LLMModel> providerModels,
        ProviderSyncChanges changes)
    {
        var existing = providerModels.FirstOrDefault(m =>
            string.Equals(m.ApiModelId, apiModel.ApiModelId, StringComparison.OrdinalIgnoreCase));

        if (existing is not null && !existing.IsDeleted)
        {
            return;
        }

        var isNonChatModel = NonChatModelPatterns.IsLikelyNonChatModel(apiModel.ApiModelId);
        var testResult = await ProbeModelAsync(provider, apiModel, existing, isNonChatModel);

        var resultWithName = testResult with { ModelName = apiModel.ModelName };

        if (existing is null)
        {
            await InsertModelAsync(provider, apiModel, testResult, isNonChatModel);
        }
        else if (testResult.Passed)
        {
            await RestoreModelAsync(provider, apiModel, existing, testResult);
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
            return;
        }

        changes.ModelTestResults.Add(resultWithName);
        changes.NewNames.Add(apiModel.ModelName);

        if (!testResult.Passed)
        {
            changes.FailedCount++;
        }
    }

    private async Task<LLMModelTestResult> ProbeModelAsync(
        ILLMProvider provider,
        LLMModelDiscovery apiModel,
        LLMModel? existing,
        bool isNonChatModel)
    {
        if (isNonChatModel)
        {
            var testResult = new LLMModelTestResult(
                apiModel.ApiModelId, apiModel.ModelName, false, NonChatModelPatterns.SkipReason, 0);

            _logger.LogDebug(
                "LLMModelSyncService - {Provider}: {ModelId} skipped, non-chat model id pattern, not tested",
                provider.ProviderName, apiModel.ApiModelId);

            return testResult;
        }

        return await provider.TestModelAsync(apiModel.ApiModelId, existing?.SupportedParameters);
    }

    private async Task InsertModelAsync(
        ILLMProvider provider,
        LLMModelDiscovery apiModel,
        LLMModelTestResult testResult,
        bool isNonChatModel)
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

    private async Task RestoreModelAsync(
        ILLMProvider provider,
        LLMModelDiscovery apiModel,
        LLMModel existing,
        LLMModelTestResult testResult)
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

    /// <summary>
    /// Soft-deletes every active model the provider no longer advertises, except the default model. An empty
    /// discovery list is treated as a provider hiccup and removes nothing.
    /// </summary>
    /// <param name="provider">Provider being synced.</param>
    /// <param name="discovered">The current discovery list of the provider.</param>
    /// <param name="providerModels">The stored models of the provider, already updated by the discovery pass.</param>
    /// <param name="deactivatedNames">Receives the names of the removed models.</param>
    private async Task RemoveVanishedModelsAsync(
        ILLMProvider provider,
        List<LLMModelDiscovery> discovered,
        List<LLMModel> providerModels,
        List<string> deactivatedNames)
    {
        if (discovered.Count == 0)
        {
            _logger.LogWarning(
                "LLMModelSyncService - {Provider}: discovery returned an empty model list, skipping removal check",
                provider.ProviderName);
            return;
        }

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

    private static LLMSyncNotification BuildNotification(ILLMProvider provider, ProviderSyncChanges changes) => new()
    {
        Id = Guid.NewGuid(),
        ProviderId = provider.ProviderId,
        ProviderName = provider.ProviderName,
        NewModelsCount = changes.NewNames.Count,
        FailedModelsCount = changes.FailedCount,
        DeactivatedModelsCount = changes.DeactivatedNames.Count,
        NewModelNames = changes.NewNames,
        DeactivatedModelNames = changes.DeactivatedNames,
        ModelTestResults = changes.ModelTestResults,
        SyncedAt = DateTime.UtcNow,
        IsRead = false,
        CreateTime = DateTime.UtcNow,
        UpdateTime = DateTime.UtcNow,
    };

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

    private sealed class ProviderSyncChanges
    {
        public List<string> NewNames { get; } = new();

        public List<string> DeactivatedNames { get; } = new();

        public List<LLMModelTestResult> ModelTestResults { get; } = new();

        public int FailedCount { get; set; }
    }
}
