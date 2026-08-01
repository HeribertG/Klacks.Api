// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes the voice settings of the assistant: which engine turns speech into text, which model and
/// prompt it uses, whether the raw transcript is cleaned up afterwards, how the assistant answers,
/// which voice it speaks with, and how it reacts when someone talks over it. API keys are written
/// here as well but are never read back by the matching read skill.
/// </summary>
/// <param name="sttEngine">Engine that turns speech into text.</param>
/// <param name="transcriptionModel">Model the engine transcribes with.</param>
/// <param name="transcriptionPrompt">Hint text that steers the transcription towards house vocabulary.</param>
/// <param name="enhancementEnabled">Whether the raw transcript is cleaned up afterwards.</param>
/// <param name="outputMode">How the assistant answers: in writing, spoken, or both.</param>
/// <param name="ttsProvider">Service that speaks the answers.</param>
/// <param name="ttsVoice">Voice the answers are spoken in.</param>
/// <param name="silenceThresholdMs">Milliseconds of silence that end an utterance.</param>
/// <param name="bargeInEnabled">Whether talking over the assistant interrupts it.</param>
/// <param name="sttApiKey">Key for the speech-to-text service.</param>
/// <param name="deepgramApiKey">Key for Deepgram.</param>
/// <param name="groqApiKey">Key for Groq.</param>
/// <param name="assemblyAiApiKey">Key for AssemblyAI.</param>
/// <param name="openAiTtsApiKey">Key for the OpenAI voice service.</param>
/// <param name="elevenLabsTtsApiKey">Key for the ElevenLabs voice service.</param>
/// <param name="googleTtsApiKey">Key for the Google voice service.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_speech_settings")]
public class UpdateSpeechSettingsSkill : SettingsWriterSkillBase
{
    public UpdateSpeechSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
        : base(settingsRepository, unitOfWork)
    {
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var pending = new List<PendingSetting>();

        CollectText(pending, parameters, "sttEngine", Settings.ASSISTANT_STT_ENGINE);
        CollectText(pending, parameters, "transcriptionModel", Settings.ASSISTANT_TRANSCRIPTION_MODEL);
        CollectText(pending, parameters, "transcriptionPrompt", Settings.ASSISTANT_TRANSCRIPTION_PROMPT);
        CollectBoolean(pending, parameters, "enhancementEnabled", Settings.ASSISTANT_ENHANCEMENT_ENABLED);
        CollectText(pending, parameters, "outputMode", Settings.ASSISTANT_OUTPUT_MODE);
        CollectText(pending, parameters, "ttsProvider", Settings.ASSISTANT_TTS_PROVIDER);
        CollectText(pending, parameters, "ttsVoice", Settings.ASSISTANT_TTS_VOICE);
        CollectInteger(pending, parameters, "silenceThresholdMs", Settings.ASSISTANT_SILENCE_THRESHOLD_MS);
        CollectBoolean(pending, parameters, "bargeInEnabled", Settings.ASSISTANT_BARGE_IN_ENABLED);
        CollectText(pending, parameters, "sttApiKey", Settings.ASSISTANT_STT_API_KEY);
        CollectText(pending, parameters, "deepgramApiKey", Settings.ASSISTANT_STT_API_KEY_DEEPGRAM);
        CollectText(pending, parameters, "groqApiKey", Settings.ASSISTANT_STT_API_KEY_GROQ);
        CollectText(pending, parameters, "assemblyAiApiKey", Settings.ASSISTANT_STT_API_KEY_ASSEMBLYAI);
        CollectText(pending, parameters, "openAiTtsApiKey", Settings.ASSISTANT_TTS_API_KEY_OPENAI);
        CollectText(pending, parameters, "elevenLabsTtsApiKey", Settings.ASSISTANT_TTS_API_KEY_ELEVENLABS);
        CollectText(pending, parameters, "googleTtsApiKey", Settings.ASSISTANT_TTS_API_KEY_GOOGLE);

        return await PersistAsync(pending, "Speech settings");
    }
}
