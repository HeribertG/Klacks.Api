// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Transcribes a complete audio buffer through a registered or self-hosted STT provider for evaluation runs.
/// A custom:{guid} id is resolved to the self-hosted provider row and transcribed through the custom session
/// factory (no API key); any other id is resolved to a registered provider, its configured API key is read and
/// decrypted from settings, then a one-shot session sends the audio and the final transcript text is returned.
/// </summary>
/// <param name="providerId">Registered STT provider id (e.g. "groq-whisper") or custom:{guid} of a self-hosted provider</param>
/// <param name="audio">Complete audio file bytes to transcribe</param>
/// <param name="language">BCP-47 language code of the recording (e.g. "de")</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class SttSpeechTranscriptionService : ISpeechTranscriptionService
{
    private readonly IEnumerable<ISttProvider> _sttProviders;
    private readonly ISettingsReader _settingsReader;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly ICustomSttProviderResolver _customProviderResolver;
    private readonly ICustomSttSessionFactory _customSessionFactory;

    public SttSpeechTranscriptionService(
        IEnumerable<ISttProvider> sttProviders,
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService,
        ICustomSttProviderResolver customProviderResolver,
        ICustomSttSessionFactory customSessionFactory)
    {
        _sttProviders = sttProviders;
        _settingsReader = settingsReader;
        _encryptionService = encryptionService;
        _customProviderResolver = customProviderResolver;
        _customSessionFactory = customSessionFactory;
    }

    public async Task<string> TranscribeAsync(string providerId, byte[] audio, string language, CancellationToken cancellationToken = default)
    {
        if (_customProviderResolver.IsCustomProviderId(providerId))
        {
            return await TranscribeWithCustomProviderAsync(providerId, audio, language, cancellationToken);
        }

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

    private async Task<string> TranscribeWithCustomProviderAsync(string providerId, byte[] audio, string language, CancellationToken cancellationToken)
    {
        var customProvider = await _customProviderResolver.TryResolveAsync(providerId, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown STT provider: {providerId}");

        await using var session = _customSessionFactory.CreateSession(customProvider, language);
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
