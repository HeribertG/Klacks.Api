// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enhances raw speech-to-text transcriptions using the configured LLM provider and domain dictionary context.
/// </summary>
/// <param name="providerFactory">Factory for resolving the LLM provider by model ID</param>
/// <param name="dictionaryService">Service for building the domain-specific terminology context</param>
/// <param name="settingsRepository">Repository for reading the transcription model setting</param>
/// <param name="logger">Logger for diagnostic output</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using TranscriptionConstants = Klacks.Api.Application.Constants.TranscriptionConstants;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class TranscriptionEnhancerService : ITranscriptionEnhancerService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IDictionaryService _dictionaryService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<TranscriptionEnhancerService> _logger;

    public TranscriptionEnhancerService(
        ILLMProviderFactory providerFactory,
        IDictionaryService dictionaryService,
        ISettingsRepository settingsRepository,
        ILogger<TranscriptionEnhancerService> logger)
    {
        _providerFactory = providerFactory;
        _dictionaryService = dictionaryService;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<string> EnhanceTranscriptionAsync(string rawText, string locale, string? modelId = null, CancellationToken ct = default)
    {
        try
        {
            var effectiveModelId = !string.IsNullOrWhiteSpace(modelId)
                ? modelId
                : await GetModelIdFromSettingsAsync();

            var provider = await _providerFactory.GetProviderForModelAsync(effectiveModelId);

            if (provider == null || !provider.IsEnabled)
            {
                _logger.LogWarning("No enabled LLM provider found for model {ModelId}", effectiveModelId);
                return rawText;
            }

            var dictionaryContext = await _dictionaryService.BuildContextAsync(ct);

            var dictionarySection = string.IsNullOrWhiteSpace(dictionaryContext)
                ? string.Empty
                : string.Format(TranscriptionConstants.DictionaryPromptSection, dictionaryContext);

            var promptTemplate = await GetPromptFromSettingsAsync();
            var systemPrompt = string.Format(promptTemplate, dictionarySection);

            var localeHint = string.IsNullOrWhiteSpace(locale) ? string.Empty : $" The input language is: {locale}.";

            var request = new LLMProviderRequest
            {
                ModelId = effectiveModelId,
                SystemPrompt = systemPrompt + localeHint,
                Message = rawText,
                Temperature = TranscriptionConstants.Temperature,
                MaxTokens = TranscriptionConstants.MaxTokens
            };

            _logger.LogInformation("Sending transcription enhancement request using model {ModelId}", effectiveModelId);

            var response = await provider.ProcessAsync(request);

            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("LLM provider returned empty or failed response for transcription enhancement");
                return rawText;
            }

            return response.Content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription enhancement failed, returning raw text");
            return rawText;
        }
    }

    private async Task<string> GetModelIdFromSettingsAsync()
    {
        var modelSetting = await _settingsRepository.GetSetting(SettingsConstants.ASSISTANT_TRANSCRIPTION_MODEL);
        return string.IsNullOrWhiteSpace(modelSetting?.Value)
            ? TranscriptionConstants.DefaultModelId
            : modelSetting.Value;
    }

    private async Task<string> GetPromptFromSettingsAsync()
    {
        var promptSetting = await _settingsRepository.GetSetting(SettingsConstants.ASSISTANT_TRANSCRIPTION_PROMPT);
        return string.IsNullOrWhiteSpace(promptSetting?.Value)
            ? TranscriptionConstants.SystemPromptTemplate
            : promptSetting.Value;
    }
}
