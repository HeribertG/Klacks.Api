// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Text-to-speech provider using the Google Cloud Text-to-Speech API. Reads its API key from the
/// dedicated TTS speech setting, falling back to the stored "google-tts" LLM provider credential.
/// </summary>
/// <param name="httpClientFactory">Factory creating the HttpClient used for the synthesis request</param>
/// <param name="apiKeyResolver">Resolves the Google TTS API key (dedicated setting or legacy fallback)</param>
/// <param name="logger">Logger for diagnostics and error tracking</param>
using System.Net.Http.Json;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class GoogleTtsService : ITtsProvider
{
    private const string AudioContentProperty = "audioContent";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITtsApiKeyResolver _apiKeyResolver;
    private readonly ILogger<GoogleTtsService> _logger;

    public string ProviderId => TtsProviderConstants.Google;

    public GoogleTtsService(
        IHttpClientFactory httpClientFactory,
        ITtsApiKeyResolver apiKeyResolver,
        ILogger<GoogleTtsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyResolver = apiKeyResolver;
        _logger = logger;
    }

    public Task<IReadOnlyList<TtsVoice>> GetVoicesAsync(CancellationToken ct = default)
    {
        var voices = GoogleTtsConstants.Voices
            .Select(kvp => new TtsVoice(
                VoiceId: kvp.Key,
                Locale: LanguageCodeFromVoice(kvp.Key),
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
                "Google Cloud TTS API key is not configured. Set the Google TTS key in the speech settings.");
        }

        var input = text.Length > GoogleTtsConstants.MaxInputLength
            ? text[..GoogleTtsConstants.MaxInputLength]
            : text;
        var voice = ResolveVoice(voiceId, locale);
        var languageCode = LanguageCodeFromVoice(voice);
        var url = $"{GoogleTtsConstants.ApiUrl}?key={apiKey}";

        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                input = new { text = input },
                voice = new { languageCode, name = voice },
                audioConfig = new { audioEncoding = GoogleTtsConstants.AudioEncoding }
            })
        };

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Google TTS request failed with status {Status}: {Error}",
                (int)response.StatusCode, error.ForLog());
            throw new InvalidOperationException($"Google TTS request failed with status {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty(AudioContentProperty, out var audioElement)
            || audioElement.GetString() is not { Length: > 0 } base64Audio)
        {
            _logger.LogWarning("Google TTS response did not contain audio content");
            return Array.Empty<byte>();
        }

        return Convert.FromBase64String(base64Audio);
    }

    private static string ResolveVoice(string voiceId, string locale)
    {
        if (!string.IsNullOrWhiteSpace(voiceId)
            && voiceId != TtsProviderConstants.AutoVoice
            && GoogleTtsConstants.Voices.ContainsKey(voiceId))
        {
            return voiceId;
        }

        var langPrefix = locale.Contains('-') ? locale[..locale.IndexOf('-')] : locale;
        return GoogleTtsConstants.LocaleDefaults.TryGetValue(langPrefix, out var localeVoice)
            ? localeVoice
            : GoogleTtsConstants.DefaultVoice;
    }

    private static string LanguageCodeFromVoice(string voice)
    {
        var parts = voice.Split('-');
        return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : GoogleTtsConstants.DefaultLanguageCode;
    }
}
