// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// AssemblyAI real-time STT provider using WebSocket streaming.
/// Sends base64-encoded audio chunks and receives interim/final transcription results.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public class AssemblyAiSttProvider : ISttProvider
{
    public string ProviderId => SttProviderConstants.AssemblyAi;

    public async Task<WebSocket> ConnectAsync(SttConfig config, CancellationToken ct = default)
    {
        var ws = new ClientWebSocket();
        var url = $"{SttProviderConstants.AssemblyAiWssUrl}?sample_rate={config.SampleRate}&token={config.ApiKey}";
        await ws.ConnectAsync(new Uri(url), ct);
        return ws;
    }

    public async Task SendAudioAsync(WebSocket ws, byte[] audioChunk, CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(audioChunk);
        var json = JsonSerializer.Serialize(new { audio_data = base64 });
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    public async Task<SttResult?> ReceiveAsync(WebSocket ws, CancellationToken ct = default)
    {
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

        if (result.MessageType == WebSocketMessageType.Close)
            return null;

        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        return ParseResult(json);
    }

    public async Task DisconnectAsync(WebSocket ws, CancellationToken ct = default)
    {
        if (ws.State == WebSocketState.Open)
        {
            var terminate = Encoding.UTF8.GetBytes("{\"terminate_session\":true}");
            await ws.SendAsync(new ArraySegment<byte>(terminate), WebSocketMessageType.Text, true, ct);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", ct);
        }
    }

    internal static SttResult? ParseResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("text", out var textProp))
            return null;

        var text = textProp.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var messageType = root.TryGetProperty("message_type", out var mt) ? mt.GetString() : "";
        var isFinal = messageType == "FinalTranscript";
        var confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetSingle() : 0.0f;

        return new SttResult(text, isFinal, confidence);
    }
}
