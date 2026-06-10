// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads a TTS provider's API key from its dedicated ASSISTANT_TTS_API_KEY_* setting (decrypting it),
/// and falls back to the stored LLM provider credential when no dedicated key is configured.
/// </summary>
/// <param name="settingsRepository">Reads the dedicated per-provider TTS API key setting</param>
/// <param name="encryptionService">Decrypts the stored setting value</param>
/// <param name="llmRepository">Provides the legacy fallback credential from the LLM provider row</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class TtsApiKeyResolver : ITtsApiKeyResolver
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly ILLMRepository _llmRepository;

    public TtsApiKeyResolver(
        ISettingsRepository settingsRepository,
        ISettingsEncryptionService encryptionService,
        ILLMRepository llmRepository)
    {
        _settingsRepository = settingsRepository;
        _encryptionService = encryptionService;
        _llmRepository = llmRepository;
    }

    public async Task<string?> ResolveAsync(string providerId, CancellationToken ct = default)
    {
        var settingType = ResolveSettingType(providerId);
        if (settingType != null)
        {
            var setting = await _settingsRepository.GetSetting(settingType);
            if (!string.IsNullOrWhiteSpace(setting?.Value))
            {
                var decrypted = _encryptionService.Decrypt(setting.Value);
                if (!string.IsNullOrWhiteSpace(decrypted))
                {
                    return decrypted;
                }
            }
        }

        var provider = await _llmRepository.GetProviderByIdAsync(providerId);
        return string.IsNullOrWhiteSpace(provider?.ApiKey) ? null : provider.ApiKey;
    }

    private static string? ResolveSettingType(string providerId) => providerId switch
    {
        TtsProviderConstants.OpenAi => Klacks.Api.Application.Constants.Settings.ASSISTANT_TTS_API_KEY_OPENAI,
        TtsProviderConstants.ElevenLabs => Klacks.Api.Application.Constants.Settings.ASSISTANT_TTS_API_KEY_ELEVENLABS,
        TtsProviderConstants.Google => Klacks.Api.Application.Constants.Settings.ASSISTANT_TTS_API_KEY_GOOGLE,
        _ => null,
    };
}
