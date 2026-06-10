// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Text-to-speech provider using the ElevenLabs API. Reads its API key from the dedicated
/// TTS speech setting, falling back to the stored "elevenlabs" LLM provider credential.
/// </summary>
/// <param name="httpClientFactory">Factory creating the HttpClient used for the synthesis request</param>
/// <param name="apiKeyResolver">Resolves the ElevenLabs TTS API key (dedicated setting or legacy fallback)</param>
/// <param name="logger">Logger for diagnostics and error tracking</param>
using System.Net.Http.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class ElevenLabsTtsService : ITtsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITtsApiKeyResolver _apiKeyResolver;
    private readonly ILogger<ElevenLabsTtsService> _logger;

    public string ProviderId => TtsProviderConstants.ElevenLabs;

    public ElevenLabsTtsService(
        IHttpClientFactory httpClientFactory,
        ITtsApiKeyResolver apiKeyResolver,
        ILogger<ElevenLabsTtsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyResolver = apiKeyResolver;
        _logger = logger;
    }

    public Task<IReadOnlyList<TtsVoice>> GetVoicesAsync(CancellationToken ct = default)
    {
        var voices = ElevenLabsTtsConstants.Voices
            .Select(kvp => new TtsVoice(
                VoiceId: kvp.Key,
                Locale: TtsProviderConstants.AutoVoice,
                DisplayName: kvp.Value))
            .ToList();
        return Task.FromResult<IReadOnlyList<TtsVoice>>(voices);
    }

    public async Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default)
    {
        var apiKey = await _apiKeyResolver.ResolveAsync(ProviderId, ct);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "ElevenLabs TTS API key is not configured. Set the ElevenLabs TTS key in the speech settings.");
        }

        var input = text.Length > ElevenLabsTtsConstants.MaxInputLength
            ? text[..ElevenLabsTtsConstants.MaxInputLength]
            : text;
        var voice = ResolveVoice(voiceId);
        var url = string.Format(ElevenLabsTtsConstants.ApiUrlTemplate, voice);

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                text = input,
                model_id = ElevenLabsTtsConstants.Model
            })
        };
        request.Headers.Add(ElevenLabsTtsConstants.ApiKeyHeader, apiKey);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("ElevenLabs TTS request failed with status {Status}: {Error}",
                (int)response.StatusCode, error.ForLog());
            throw new InvalidOperationException($"ElevenLabs TTS request failed with status {(int)response.StatusCode}.");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private static string ResolveVoice(string voiceId)
    {
        if (string.IsNullOrWhiteSpace(voiceId) || voiceId == TtsProviderConstants.AutoVoice)
        {
            return ElevenLabsTtsConstants.DefaultVoiceId;
        }

        return ElevenLabsTtsConstants.Voices.ContainsKey(voiceId)
            ? voiceId
            : ElevenLabsTtsConstants.DefaultVoiceId;
    }
}
