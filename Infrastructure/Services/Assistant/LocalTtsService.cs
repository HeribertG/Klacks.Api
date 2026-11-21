// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Text-to-speech provider "local": speaks through the Supertonic 3 voice of Klacks.Stt inside the installation, so no
/// text leaves the house. The Klacks.Stt address comes from ASSISTANT_TTS_LOCAL_BASE_URL, which the updater writes after
/// a successful installation. Texts longer than Klacks.Stt accepts are split at sentence boundaries, each piece is sent
/// to POST /v1/audio/speech and the WAV answers are joined into one file. A language the model does not speak
/// (unsupported_language, e.g. Maltese) yields an empty result ("no audio"); a missing installation, a missing model or
/// an unreachable service throws the not-configured error; a busy service (429) throws TtsProviderBusyException with
/// the Retry-After delay it named. There is no fallback to a cloud provider.
/// </summary>
/// <param name="httpClientFactory">Creates the named client with the long synthesis timeout</param>
/// <param name="settingsReader">Reads the Klacks.Stt address of the local voice</param>
/// <param name="logger">Logger for diagnostics</param>
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class LocalTtsService : ITtsProvider
{
    private const string ErrorProperty = "error";
    private const string CodeProperty = "code";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<LocalTtsService> _logger;

    public LocalTtsService(
        IHttpClientFactory httpClientFactory,
        ISettingsReader settingsReader,
        ILogger<LocalTtsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public string ProviderId => TtsProviderConstants.Local;

    public string MediaType => TtsProviderConstants.WavMediaType;

    public bool ChunksInternally => true;

    public Task<IReadOnlyList<TtsVoice>> GetVoicesAsync(CancellationToken ct = default)
    {
        var voices = LocalTtsConstants.VoiceNames
            .Select(name => new TtsVoice(
                VoiceId: LocalTtsConstants.VoicePrefix + name,
                Locale: TtsProviderConstants.AutoVoice,
                DisplayName: name))
            .ToList();
        return Task.FromResult<IReadOnlyList<TtsVoice>>(voices);
    }

    public async Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default)
    {
        var baseUrl = (await _settingsReader.GetSetting(Klacks.Api.Application.Constants.Settings.ASSISTANT_TTS_LOCAL_BASE_URL))?.Value;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(LocalTtsConstants.NotInstalledMessage);
        }

        var url = baseUrl.TrimEnd('/') + LocalTtsConstants.SpeechPath;
        var voice = ResolveVoice(voiceId);
        var language = ResolveLanguage(locale);
        var client = _httpClientFactory.CreateClient(LocalTtsConstants.HttpClientName);

        var pieces = new List<byte[]>();
        foreach (var chunk in TtsTextChunker.Split(text, LocalTtsConstants.MaxRequestTextLength))
        {
            if (string.IsNullOrWhiteSpace(chunk))
            {
                continue;
            }

            var audio = await SynthesizePieceAsync(client, url, chunk, voice, language, ct);
            if (audio is null)
            {
                return [];
            }

            pieces.Add(audio);
        }

        return WavConcatenator.Concatenate(pieces);
    }

    private async Task<byte[]?> SynthesizePieceAsync(
        HttpClient client, string url, string input, string voice, string language, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(url, new { input, voice, language }, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Local TTS service at {Url} is not reachable.", url.ForLog());
            throw new InvalidOperationException(LocalTtsConstants.NotInstalledMessage, ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(ct);
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var code = ReadErrorCode(body);

            if (response.StatusCode == HttpStatusCode.BadRequest
                && string.Equals(code, LocalTtsConstants.ErrorUnsupportedLanguage, StringComparison.Ordinal))
            {
                _logger.LogInformation("Local TTS does not speak language {Language}; no audio is produced.", language.ForLog());
                return null;
            }

            _logger.LogError("Local TTS request failed with status {Status} and error code {Code}.", (int)response.StatusCode, code.ForLog());

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new TtsProviderBusyException(response.Headers.RetryAfter?.Delta);
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable
                && string.Equals(code, LocalTtsConstants.ErrorModelNotInstalled, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(LocalTtsConstants.NotInstalledMessage);
            }

            throw new InvalidOperationException($"Local TTS request failed with status {(int)response.StatusCode}.");
        }
    }

    private static string? ReadErrorCode(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty(ErrorProperty, out var error)
                && error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty(CodeProperty, out var code)
                && code.ValueKind == JsonValueKind.String
                    ? code.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ResolveVoice(string voiceId) =>
        LocalTtsConstants.VoiceIds.FirstOrDefault(v => string.Equals(v, voiceId, StringComparison.OrdinalIgnoreCase))
            ?? LocalTtsConstants.DefaultVoiceId;

    private static string ResolveLanguage(string locale)
    {
        var value = string.IsNullOrWhiteSpace(locale) ? TtsProviderConstants.DefaultLocale : locale.Trim();
        return (value.Length > LocalTtsConstants.LanguageCodeLength
            ? value[..LocalTtsConstants.LanguageCodeLength]
            : value).ToLowerInvariant();
    }
}
