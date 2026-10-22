// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the current speech (STT/TTS) settings: STT engine, transcription enhancement model
/// and flag, output mode, TTS provider and voice, silence threshold, barge-in flag. For API
/// keys it returns only whether a key is configured, never the key itself. When the stored engine is the
/// installed self-hosted speech recognition it is reported as "local" together with the installed engine alias.
/// </summary>
/// <param name="settingsRepository">Reads the stored speech settings</param>
/// <param name="customSttProviderResolver">Resolves the stored custom engine id to the enabled local provider</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_speech_settings")]
public class GetSpeechSettingsSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICustomSttProviderResolver _customSttProviderResolver;

    public GetSpeechSettingsSkill(
        ISettingsRepository settingsRepository,
        ICustomSttProviderResolver customSttProviderResolver)
    {
        _settingsRepository = settingsRepository;
        _customSttProviderResolver = customSttProviderResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var sttEngine = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_ENGINE);
        var transcriptionModel = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TRANSCRIPTION_MODEL);
        var enhancementEnabled = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_ENHANCEMENT_ENABLED);
        var outputMode = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_OUTPUT_MODE);
        var ttsProvider = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_PROVIDER);
        var ttsVoice = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_VOICE);
        var silenceThreshold = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_SILENCE_THRESHOLD_MS);
        var bargeInEnabled = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_BARGE_IN_ENABLED);
        var sttApiKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY);
        var deepgramKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY_DEEPGRAM);
        var groqKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY_GROQ);
        var assemblyAiKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY_ASSEMBLYAI);
        var openAiTtsKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_API_KEY_OPENAI);
        var elevenLabsTtsKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_API_KEY_ELEVENLABS);
        var googleTtsKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_API_KEY_GOOGLE);

        var (reportedEngine, localEngine) = await ResolveEngineAsync(sttEngine?.Value, cancellationToken);

        var resultData = new
        {
            SttEngine = reportedEngine,
            LocalEngine = localEngine,
            TranscriptionModel = transcriptionModel?.Value ?? "",
            EnhancementEnabled = enhancementEnabled?.Value ?? "",
            OutputMode = outputMode?.Value ?? "",
            TtsProvider = ttsProvider?.Value ?? "",
            TtsVoice = ttsVoice?.Value ?? "",
            SilenceThresholdMs = silenceThreshold?.Value ?? "",
            BargeInEnabled = bargeInEnabled?.Value ?? "",
            HasSttApiKey = !string.IsNullOrWhiteSpace(sttApiKey?.Value),
            HasDeepgramApiKey = !string.IsNullOrWhiteSpace(deepgramKey?.Value),
            HasGroqApiKey = !string.IsNullOrWhiteSpace(groqKey?.Value),
            HasAssemblyAiApiKey = !string.IsNullOrWhiteSpace(assemblyAiKey?.Value),
            HasOpenAiTtsApiKey = !string.IsNullOrWhiteSpace(openAiTtsKey?.Value),
            HasElevenLabsTtsApiKey = !string.IsNullOrWhiteSpace(elevenLabsTtsKey?.Value),
            HasGoogleTtsApiKey = !string.IsNullOrWhiteSpace(googleTtsKey?.Value)
        };

        return SkillResult.SuccessResult(resultData, "Speech settings retrieved successfully.");
    }

    private async Task<(string Engine, string LocalEngine)> ResolveEngineAsync(
        string? storedEngine, CancellationToken cancellationToken)
    {
        var engine = storedEngine ?? "";
        if (!string.Equals(engine, WhisperPluginConstants.ProviderEngineValue, StringComparison.OrdinalIgnoreCase))
        {
            return (engine, "");
        }

        var provider = await _customSttProviderResolver.TryResolveAsync(engine, cancellationToken);
        if (provider is null)
        {
            return (engine, "");
        }

        return (WhisperPluginConstants.LocalEngineValue,
            WhisperPluginConstants.ResolveModelAlias(provider.LanguageModel) ?? "");
    }
}
