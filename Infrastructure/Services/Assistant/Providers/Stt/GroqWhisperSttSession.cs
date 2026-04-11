// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-connection Groq Whisper STT session that buffers audio and transcribes via REST when ReceiveAsync is called.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients</param>
/// <param name="config">STT configuration including API key and language</param>
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
    private readonly List<byte> _audioBuffer = [];

    public GroqWhisperSttSession(IHttpClientFactory httpClientFactory, SttConfig config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
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
        content.Add(audioContent, "file", "audio.wav");
        content.Add(new StringContent("whisper-large-v3"), "model");
        content.Add(new StringContent(_config.Language), "language");

        var response = await client.PostAsync(SttProviderConstants.GroqWhisperRestUrl, content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        _audioBuffer.Clear();

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("text").GetString() ?? "";

        return string.IsNullOrWhiteSpace(text) ? null : new SttResult(text, true, 1.0f);
    }

    public ValueTask DisposeAsync()
    {
        _audioBuffer.Clear();
        return ValueTask.CompletedTask;
    }
}
