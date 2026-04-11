// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-connection AssemblyAI STT session that owns a WebSocket and handles base64-encoded audio streaming.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class AssemblyAiSttSession : ISttSession
{
    private readonly ClientWebSocket _ws;

    public AssemblyAiSttSession(ClientWebSocket ws)
    {
        _ws = ws;
    }

    public async Task SendAudioAsync(byte[] audioChunk, CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(audioChunk);
        var json = JsonSerializer.Serialize(new { audio_data = base64 });
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    public async Task<SttResult?> ReceiveAsync(CancellationToken ct = default)
    {
        var (type, text) = await SttWebSocketHelper.ReceiveFullMessageAsync(_ws, ct);
        if (type == WebSocketMessageType.Close)
            return null;
        return AssemblyAiSttProvider.ParseResult(text);
    }

    public async ValueTask DisposeAsync()
    {
        if (_ws.State == WebSocketState.Open)
        {
            var terminate = Encoding.UTF8.GetBytes("{\"terminate_session\":true}");
            await _ws.SendAsync(new ArraySegment<byte>(terminate), WebSocketMessageType.Text, true, CancellationToken.None);
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
        }
        _ws.Dispose();
    }
}
