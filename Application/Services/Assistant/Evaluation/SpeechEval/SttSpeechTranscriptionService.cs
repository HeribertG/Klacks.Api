// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Transcribes a complete audio buffer through a registered STT provider for evaluation runs.
/// Resolves the provider by its id, reads and decrypts the configured API key from settings,
/// opens a one-shot session, sends the audio and returns the final transcript text.
/// </summary>
/// <param name="providerId">Registered STT provider id (e.g. "groq-whisper")</param>
/// <param name="audio">Complete audio file bytes to transcribe</param>
/// <param name="language">BCP-47 language code of the recording (e.g. "de")</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class SttSpeechTranscriptionService : ISpeechTranscriptionService
{
    private readonly IEnumerable<ISttProvider> _sttProviders;
    private readonly ISettingsReader _settingsReader;
    private readonly ISettingsEncryptionService _encryptionService;

    public SttSpeechTranscriptionService(
        IEnumerable<ISttProvider> sttProviders,
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService)
    {
        _sttProviders = sttProviders;
        _settingsReader = settingsReader;
        _encryptionService = encryptionService;
    }

    public async Task<string> TranscribeAsync(string providerId, byte[] audio, string language, CancellationToken cancellationToken = default)
    {
        var provider = _sttProviders.FirstOrDefault(p => p.ProviderId == providerId)
            ?? throw new InvalidOperationException($"Unknown STT provider: {providerId}");

        var apiKey = await ReadApiKeyAsync(providerId)
            ?? throw new InvalidOperationException($"No API key configured for STT provider: {providerId}");

        var config = new SttConfig(apiKey, language);

        await using var session = await provider.CreateSessionAsync(config, cancellationToken);
        await session.SendAudioAsync(audio, cancellationToken);
        var result = await session.ReceiveAsync(cancellationToken);

        return result?.Text ?? string.Empty;
    }

    private async Task<string?> ReadApiKeyAsync(string providerId)
    {
        var settingType = ResolveApiKeySettingType(providerId);
        if (settingType == null)
        {
            return null;
        }

        var setting = await _settingsReader.GetSetting(settingType);
        if (string.IsNullOrWhiteSpace(setting?.Value))
        {
            return null;
        }

        return _encryptionService.Decrypt(setting.Value);
    }

    private static string? ResolveApiKeySettingType(string providerId) => providerId switch
    {
        SttProviderConstants.Deepgram => Settings.ASSISTANT_STT_API_KEY_DEEPGRAM,
        SttProviderConstants.GroqWhisper => Settings.ASSISTANT_STT_API_KEY_GROQ,
        SttProviderConstants.AssemblyAi => Settings.ASSISTANT_STT_API_KEY_ASSEMBLYAI,
        _ => null,
    };
}
