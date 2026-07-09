// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// STT session for a custom OpenAI-compatible REST endpoint (e.g. self-hosted faster-whisper).
/// Buffers audio and transcribes the full utterance via a single multipart request when ReceiveAsync is called.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients</param>
/// <param name="provider">Custom provider configuration (base URL, optional API key, model id)</param>
/// <param name="locale">Requested transcription locale, mapped to a Whisper language code</param>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.Http.Headers;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class CustomRestSttSession : ISttSession
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CustomSttProvider _provider;
    private readonly string _locale;
    private readonly List<byte> _audioBuffer = [];

    public CustomRestSttSession(IHttpClientFactory httpClientFactory, CustomSttProvider provider, string locale)
    {
        _httpClientFactory = httpClientFactory;
        _provider = provider;
        _locale = locale;
    }

    public Task SendAudioAsync(byte[] audioChunk, CancellationToken ct = default)
    {
        _audioBuffer.AddRange(audioChunk);
        return Task.CompletedTask;
    }

    public async Task<SttResult?> ReceiveAsync(CancellationToken ct = default)
    {
        if (_audioBuffer.Count == 0)
            return null;

        var client = _httpClientFactory.CreateClient();
        if (!string.IsNullOrWhiteSpace(_provider.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _provider.ApiKey);
        }

        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(_audioBuffer.ToArray());
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, "file", "audio.wav");
        content.Add(new StringContent(ResolveModel()), "model");

        var language = WhisperLanguageMapper.ToWhisperLanguage(_locale);
        if (!string.IsNullOrWhiteSpace(language))
        {
            content.Add(new StringContent(language), "language");
        }

        var response = await client.PostAsync(BuildTranscriptionsUrl(_provider.ApiUrl), content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        _audioBuffer.Clear();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Custom STT request failed ({(int)response.StatusCode}): {json}");
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("text", out var textElement))
        {
            return null;
        }

        var text = textElement.GetString() ?? string.Empty;
        return string.IsNullOrWhiteSpace(text) ? null : new SttResult(text, true, 1.0f);
    }

    internal static string BuildTranscriptionsUrl(string apiUrl)
    {
        var trimmed = apiUrl.TrimEnd('/');
        return trimmed.EndsWith(SttProviderConstants.OpenAiTranscriptionsPath, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : trimmed + SttProviderConstants.OpenAiTranscriptionsPath;
    }

    private string ResolveModel()
    {
        return string.IsNullOrWhiteSpace(_provider.LanguageModel)
            ? SttProviderConstants.DefaultCustomWhisperModel
            : _provider.LanguageModel;
    }

    public ValueTask DisposeAsync()
    {
        _audioBuffer.Clear();
        return ValueTask.CompletedTask;
    }
}
