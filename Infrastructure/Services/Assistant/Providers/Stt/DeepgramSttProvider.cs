// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deepgram real-time STT provider using WebSocket streaming.
/// Sends raw PCM audio chunks and receives interim/final transcription results.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public class DeepgramSttProvider : ISttProvider
{
    public string ProviderId => SttProviderConstants.Deepgram;

    public async Task<WebSocket> ConnectAsync(SttConfig config, CancellationToken ct = default)
    {
        var ws = new ClientWebSocket();
        var url = $"{SttProviderConstants.DeepgramWssUrl}?model=nova-2&language={config.Language}&encoding=linear16&sample_rate={config.SampleRate}&interim_results=true&punctuate=true";
        ws.Options.SetRequestHeader("Authorization", $"Token {config.ApiKey}");
        await ws.ConnectAsync(new Uri(url), ct);
        return ws;
    }

    public async Task SendAudioAsync(WebSocket ws, byte[] audioChunk, CancellationToken ct = default)
    {
        await ws.SendAsync(new ArraySegment<byte>(audioChunk), WebSocketMessageType.Binary, true, ct);
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
            var closeMessage = Encoding.UTF8.GetBytes("{\"type\":\"CloseStream\"}");
            await ws.SendAsync(new ArraySegment<byte>(closeMessage), WebSocketMessageType.Text, true, ct);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", ct);
        }
    }

    public static SttResult? ParseResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "Results")
            return null;

        var alternatives = root.GetProperty("channel").GetProperty("alternatives");
        if (alternatives.GetArrayLength() == 0)
            return null;

        var first = alternatives[0];
        var transcript = first.GetProperty("transcript").GetString() ?? "";
        if (string.IsNullOrWhiteSpace(transcript))
            return null;

        var confidence = first.GetProperty("confidence").GetSingle();
        var isFinal = root.GetProperty("is_final").GetBoolean();

        return new SttResult(transcript, isFinal, confidence);
    }
}
