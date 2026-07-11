// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-connection Groq Whisper STT session that buffers audio and transcribes via REST when ReceiveAsync is called.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients</param>
/// <param name="config">STT configuration including API key and language</param>
/// <param name="dictionaryService">Supplies transcription dictionary terms for the Whisper bias prompt</param>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.Http.Headers;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class GroqWhisperSttSession : ISttSession
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SttConfig _config;
    private readonly IDictionaryService _dictionaryService;
    private readonly List<byte> _audioBuffer = [];

    public GroqWhisperSttSession(IHttpClientFactory httpClientFactory, SttConfig config, IDictionaryService dictionaryService)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _dictionaryService = dictionaryService;
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(_audioBuffer.ToArray());
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, SttProviderConstants.FormFieldFile, "audio.wav");
        content.Add(new StringContent(SttProviderConstants.GroqWhisperModel), SttProviderConstants.FormFieldModel);

        var language = WhisperLanguageMapper.ToWhisperLanguage(_config.Language);
        if (!string.IsNullOrWhiteSpace(language))
        {
            content.Add(new StringContent(language), SttProviderConstants.FormFieldLanguage);
            var dictionaryTerms = await _dictionaryService.GetCorrectTermsAsync(language, ct);
            content.Add(new StringContent(WhisperDomainPromptProvider.BuildPrompt(language, dictionaryTerms)), SttProviderConstants.FormFieldPrompt);
        }

        var response = await client.PostAsync(SttProviderConstants.GroqWhisperRestUrl, content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        _audioBuffer.Clear();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Groq STT request failed ({(int)response.StatusCode}): {json}");
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("text", out var textElement))
        {
            return null;
        }

        var text = textElement.GetString() ?? string.Empty;
        return string.IsNullOrWhiteSpace(text) ? null : new SttResult(text, true, 1.0f);
    }

    public ValueTask DisposeAsync()
    {
        _audioBuffer.Clear();
        return ValueTask.CompletedTask;
    }
}
