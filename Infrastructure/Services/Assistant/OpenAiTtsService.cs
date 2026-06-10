// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Text-to-speech provider using the OpenAI audio/speech API. Reads its API key from the
/// dedicated TTS speech setting, falling back to the OpenAI LLM provider credential.
/// </summary>
/// <param name="httpClientFactory">Factory creating the HttpClient used for the synthesis request</param>
/// <param name="apiKeyResolver">Resolves the OpenAI TTS API key (dedicated setting or legacy fallback)</param>
/// <param name="logger">Logger for diagnostics and error tracking</param>
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class OpenAiTtsService : ITtsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITtsApiKeyResolver _apiKeyResolver;
    private readonly ILogger<OpenAiTtsService> _logger;

    public string ProviderId => TtsProviderConstants.OpenAi;

    public OpenAiTtsService(
        IHttpClientFactory httpClientFactory,
        ITtsApiKeyResolver apiKeyResolver,
        ILogger<OpenAiTtsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyResolver = apiKeyResolver;
        _logger = logger;
    }

    public Task<IReadOnlyList<TtsVoice>> GetVoicesAsync(CancellationToken ct = default)
    {
        var voices = OpenAiTtsConstants.Voices
            .Select(v => new TtsVoice(
                VoiceId: v,
                Locale: TtsProviderConstants.AutoVoice,
                DisplayName: char.ToUpperInvariant(v[0]) + v[1..]))
            .ToList();
        return Task.FromResult<IReadOnlyList<TtsVoice>>(voices);
    }

    public async Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default)
    {
        var apiKey = await _apiKeyResolver.ResolveAsync(ProviderId, ct);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI TTS API key is not configured. Set the OpenAI TTS key in the speech settings.");
        }

        var input = text.Length > OpenAiTtsConstants.MaxInputLength
            ? text[..OpenAiTtsConstants.MaxInputLength]
            : text;
        var voice = ResolveVoice(voiceId);

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, OpenAiTtsConstants.ApiUrl)
        {
            Content = JsonContent.Create(new
            {
                model = OpenAiTtsConstants.Model,
                input,
                voice,
                response_format = OpenAiTtsConstants.ResponseFormat
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("OpenAI TTS request failed with status {Status}: {Error}",
                (int)response.StatusCode, error.ForLog());
            throw new InvalidOperationException($"OpenAI TTS request failed with status {(int)response.StatusCode}.");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private static string ResolveVoice(string voiceId)
    {
        if (string.IsNullOrWhiteSpace(voiceId) || voiceId == TtsProviderConstants.AutoVoice)
        {
            return OpenAiTtsConstants.DefaultVoice;
        }

        return OpenAiTtsConstants.Voices.Contains(voiceId, StringComparer.OrdinalIgnoreCase)
            ? voiceId.ToLowerInvariant()
            : OpenAiTtsConstants.DefaultVoice;
    }
}
