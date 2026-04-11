// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-connection Deepgram STT session that owns a WebSocket and handles audio streaming.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class DeepgramSttSession : ISttSession
{
    private readonly ClientWebSocket _ws;

    public DeepgramSttSession(ClientWebSocket ws)
    {
        _ws = ws;
    }

    public async Task SendAudioAsync(byte[] audioChunk, CancellationToken ct = default)
    {
        await _ws.SendAsync(new ArraySegment<byte>(audioChunk), WebSocketMessageType.Binary, true, ct);
    }

    public async Task<SttResult?> ReceiveAsync(CancellationToken ct = default)
    {
        var (type, text) = await SttWebSocketHelper.ReceiveFullMessageAsync(_ws, ct);
        if (type == WebSocketMessageType.Close)
            return null;
        return DeepgramSttProvider.ParseResult(text);
    }

    public async ValueTask DisposeAsync()
    {
        if (_ws.State == WebSocketState.Open)
        {
            var closeMessage = Encoding.UTF8.GetBytes("{\"type\":\"CloseStream\"}");
            await _ws.SendAsync(new ArraySegment<byte>(closeMessage), WebSocketMessageType.Text, true, CancellationToken.None);
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
        }
        _ws.Dispose();
    }
}
