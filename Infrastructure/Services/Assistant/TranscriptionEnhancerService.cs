// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enhances raw speech-to-text transcriptions using the configured LLM provider and domain dictionary context.
/// The LLM cleanup runs only for the browser STT engine; server-side engines return the regex-preprocessed text.
/// </summary>
/// <param name="providerFactory">Factory for resolving the LLM provider by model ID</param>
/// <param name="dictionaryService">Service for building the domain-specific terminology context</param>
/// <param name="settingsRepository">Repository for reading the transcription model setting</param>
/// <param name="llmRepository">Repository for translating the internal model ID into the provider-side API model ID</param>
/// <param name="logger">Logger for diagnostic output</param>

using System.Text.RegularExpressions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Services.Assistant.Providers;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using SttConstants = Klacks.Api.Application.Constants.SttProviderConstants;
using TranscriptionConstants = Klacks.Api.Application.Constants.TranscriptionConstants;
using TranscriptionExamples = Klacks.Api.Application.Constants.TranscriptionExamples;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class TranscriptionEnhancerService : ITranscriptionEnhancerService
{
    private const string WordBoundFillerPattern =
        @"\b(ähm?|ehm?|hm+|mhm|halt|sozusagen|quasi|uh+m?|um+|erm|euh|ben|bah|cioè|beh" +
        @"|este|pues|o\s+sea|né|tipo|hã|ãh|nou|yyy|eee|prostě|jakoby|ăă|păi" +
        @"|öh+m?|öö+|øh+m?|liksom|alltså|altså|niinku|tota|anu|gitu|ừm|kiểu" +
        @"|כאילו|يعني)\b";

    private const string UnboundedFillerPattern =
        @"(えーと|えっと|ええと|あのー|そのー|嗯|那个|那個|就是说|就是說|그러니까|εε|χμ|เอ่อ)";

    private static readonly Regex DisfluencyRegex = new(
        $"{WordBoundFillerPattern}|{UnboundedFillerPattern}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RepeatedWordRegex = new(
        @"\b(\p{L}{2,})\s+\1\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DigitRegex = new(
        @"\d",
        RegexOptions.Compiled);

    private readonly ILLMProviderFactory _providerFactory;
    private readonly IDictionaryService _dictionaryService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILLMRepository _llmRepository;
    private readonly ILogger<TranscriptionEnhancerService> _logger;

    public TranscriptionEnhancerService(
        ILLMProviderFactory providerFactory,
        IDictionaryService dictionaryService,
        ISettingsRepository settingsRepository,
        ILLMRepository llmRepository,
        ILogger<TranscriptionEnhancerService> logger)
    {
        _providerFactory = providerFactory;
        _dictionaryService = dictionaryService;
        _settingsRepository = settingsRepository;
        _llmRepository = llmRepository;
        _logger = logger;
    }

    public async Task<string> EnhanceTranscriptionAsync(string rawText, string locale, string? modelId = null, CancellationToken ct = default)
    {
        var preprocessed = await _dictionaryService.ApplyReplacementsAsync(rawText, locale, ct);

        if (!RequiresLlmCleanup(preprocessed))
        {
            return preprocessed;
        }

        try
        {
            if (!await IsBrowserSttEngineAsync())
            {
                _logger.LogDebug("Skipping LLM transcription enhancement because the STT engine is server-side");
                return preprocessed;
            }

            var effectiveModelId = !string.IsNullOrWhiteSpace(modelId)
                ? modelId
                : await GetModelIdFromSettingsAsync();

            var provider = await _providerFactory.GetProviderForModelAsync(effectiveModelId);

            if (provider == null || !provider.IsEnabled)
            {
                _logger.LogWarning("No enabled LLM provider found for model {ModelId}", effectiveModelId.ForLog());
                return preprocessed;
            }

            var model = await _llmRepository.GetModelByIdAsync(effectiveModelId);
            var apiModelId = string.IsNullOrWhiteSpace(model?.ApiModelId) ? effectiveModelId : model.ApiModelId;

            var dictionaryContext = await _dictionaryService.BuildContextAsync(ct);

            var dictionarySection = string.IsNullOrWhiteSpace(dictionaryContext)
                ? string.Empty
                : string.Format(TranscriptionConstants.DictionaryPromptSection, dictionaryContext);

            var promptTemplate = await GetPromptFromSettingsAsync();
            var examplesSection = TranscriptionExamples.GetForLocale(locale);
            var systemPrompt = string.Format(promptTemplate, dictionarySection) + examplesSection;

            var localeHint = string.IsNullOrWhiteSpace(locale) ? string.Empty : $" The input language is: {locale}.";

            var request = new LLMProviderRequest
            {
                ModelId = apiModelId,
                SystemPrompt = systemPrompt + localeHint,
                Message = preprocessed,
                Temperature = TranscriptionConstants.Temperature,
                MaxTokens = TranscriptionConstants.MaxTokens,
                ThinkingBudgetTokens = 0
            };

            _logger.LogInformation("Sending transcription enhancement request using model {ModelId}", apiModelId.ForLog());

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TranscriptionConstants.EnhancementTimeout);

            var response = await provider.ProcessAsync(request, cts.Token);

            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("LLM provider returned empty or failed response for transcription enhancement");
                return preprocessed;
            }

            var enhanced = response.Content.Trim();
            if (!IsPlausibleEnhancement(preprocessed, enhanced))
            {
                _logger.LogWarning(
                    "Enhanced text implausibly long ({EnhancedLength} chars from {SourceLength} chars input), returning preprocessed text",
                    enhanced.Length,
                    preprocessed.Length);
                return preprocessed;
            }

            return enhanced;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Transcription enhancement timed out after {TimeoutSeconds}s, returning preprocessed text",
                TranscriptionConstants.EnhancementTimeout.TotalSeconds);
            return preprocessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription enhancement failed, returning preprocessed text");
            return preprocessed;
        }
    }

    private static bool RequiresLlmCleanup(string text)
    {
        var trimmed = text.Trim();
        if (DigitRegex.IsMatch(trimmed))
        {
            return true;
        }

        if (trimmed.Length > TranscriptionConstants.MaxCharsForCleanupSkip)
        {
            return true;
        }

        var wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > TranscriptionConstants.MaxWordsForCleanupSkip)
        {
            return true;
        }

        return DisfluencyRegex.IsMatch(trimmed) || RepeatedWordRegex.IsMatch(trimmed);
    }

    private static bool IsPlausibleEnhancement(string source, string candidate)
    {
        var maxLength = (int)(source.Length * TranscriptionConstants.MaxEnhancedGrowthRatio)
            + TranscriptionConstants.EnhancedGrowthSlackChars;
        return candidate.Length <= maxLength;
    }

    private async Task<bool> IsBrowserSttEngineAsync()
    {
        var engineSetting = await _settingsRepository.GetSetting(SettingsConstants.ASSISTANT_STT_ENGINE);
        return string.IsNullOrWhiteSpace(engineSetting?.Value)
            || string.Equals(engineSetting.Value.Trim(), SttConstants.Browser, StringComparison.OrdinalIgnoreCase);
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
