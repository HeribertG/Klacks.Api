// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Groq Whisper STT provider using REST batch transcription.
/// Buffers audio chunks and sends them as a single request when ReceiveAsync is called.
/// </summary>
/// <param name="httpClientFactory">Factory for creating HTTP clients</param>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public class GroqWhisperSttProvider : ISttProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly List<byte> _audioBuffer = [];
    private SttConfig? _config;

    public string ProviderId => SttProviderConstants.GroqWhisper;

    public GroqWhisperSttProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<WebSocket> ConnectAsync(SttConfig config, CancellationToken ct = default)
    {
        _config = config;
        _audioBuffer.Clear();
        return Task.FromResult<WebSocket>(null!);
    }

    public Task SendAudioAsync(WebSocket ws, byte[] audioChunk, CancellationToken ct = default)
    {
        _audioBuffer.AddRange(audioChunk);
        return Task.CompletedTask;
    }

    public async Task<SttResult?> ReceiveAsync(WebSocket ws, CancellationToken ct = default)
    {
        if (_audioBuffer.Count == 0 || _config == null)
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

    public Task DisconnectAsync(WebSocket ws, CancellationToken ct = default)
    {
        _audioBuffer.Clear();
        _config = null;
        return Task.CompletedTask;
    }
}
