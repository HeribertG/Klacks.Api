// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Assistant;

/// <summary>
/// Synchronizes LLM models by querying each enabled provider's discovery API,
/// inserting new models and disabling removed ones.
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

        var existingModels = await _repository.GetModelsAsync(false);
        var providerModels = existingModels
            .Where(m => m.ProviderId == provider.ProviderId)
            .ToList();

        var newNames = new List<string>();
        var deactivatedNames = new List<string>();
        var failedCount = 0;
        var modelTestResults = new List<LLMModelTestResult>();

        foreach (var apiModel in discovered)
        {
            var existing = providerModels.FirstOrDefault(m =>
                string.Equals(m.ApiModelId, apiModel.ApiModelId, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                var testResult = await provider.TestModelAsync(apiModel.ApiModelId);
                var resultWithName = testResult with { ModelName = apiModel.ModelName };
                modelTestResults.Add(resultWithName);

                var newModel = new LLMModel
                {
                    Id = Guid.NewGuid(),
                    ModelId = GenerateModelId(apiModel.ApiModelId),
                    ModelName = apiModel.ModelName,
                    ApiModelId = apiModel.ApiModelId,
                    ProviderId = provider.ProviderId,
                    IsEnabled = testResult.Passed,
                    IsDefault = false,
                    MaxTokens = 4096,
                    ContextWindow = 128000,
                    CostPerInputToken = 0,
                    CostPerOutputToken = 0,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow,
                };

                await _repository.CreateModelAsync(newModel);
                newNames.Add(apiModel.ModelName);

                if (testResult.Passed)
                {
                    _logger.LogInformation(
                        "LLMModelSyncService - {Provider}: inserted {ModelId} (test passed in {Ms}ms)",
                        provider.ProviderName, apiModel.ApiModelId, testResult.DurationMs);
                }
                else
                {
                    failedCount++;
                    _logger.LogWarning(
                        "LLMModelSyncService - {Provider}: inserted {ModelId} as DISABLED (test failed: {Error})",
                        provider.ProviderName, apiModel.ApiModelId, testResult.ErrorMessage);
                }
            }
        }

        var discoveredIds = discovered
            .Select(m => m.ApiModelId.ToLowerInvariant())
            .ToHashSet();

        foreach (var existing in providerModels.Where(m => m.IsEnabled))
        {
            if (!discoveredIds.Contains(existing.ApiModelId.ToLowerInvariant()))
            {
                existing.IsEnabled = false;
                existing.UpdateTime = DateTime.UtcNow;
                await _repository.UpdateModelAsync(existing);
                deactivatedNames.Add(existing.ModelName);

                _logger.LogInformation("LLMModelSyncService - {Provider}: deactivated model {ModelId}",
                    provider.ProviderName, existing.ApiModelId);
            }
        }

        if (newNames.Count == 0 && deactivatedNames.Count == 0)
        {
            _logger.LogInformation("LLMModelSyncService - {Provider} sync: no changes", provider.ProviderName);
            return;
        }

        _logger.LogInformation(
            "LLMModelSyncService - {Provider} sync: {New} new ({Failed} disabled), {Deactivated} deactivated",
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
