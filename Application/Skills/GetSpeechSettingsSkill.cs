// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the current speech (STT/TTS) settings: STT engine, transcription enhancement model
/// and flag, output mode, TTS provider and voice, silence threshold. For API keys it returns
/// only whether a key is configured, never the key itself.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_speech_settings")]
public class GetSpeechSettingsSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;

    public GetSpeechSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
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
        var sttApiKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY);
        var deepgramKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY_DEEPGRAM);
        var groqKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY_GROQ);
        var assemblyAiKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_STT_API_KEY_ASSEMBLYAI);
        var openAiTtsKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_API_KEY_OPENAI);
        var elevenLabsTtsKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_API_KEY_ELEVENLABS);
        var googleTtsKey = await _settingsRepository.GetSetting(Constants.Settings.ASSISTANT_TTS_API_KEY_GOOGLE);

        var resultData = new
        {
            SttEngine = sttEngine?.Value ?? "",
            TranscriptionModel = transcriptionModel?.Value ?? "",
            EnhancementEnabled = enhancementEnabled?.Value ?? "",
            OutputMode = outputMode?.Value ?? "",
            TtsProvider = ttsProvider?.Value ?? "",
            TtsVoice = ttsVoice?.Value ?? "",
            SilenceThresholdMs = silenceThreshold?.Value ?? "",
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
}
