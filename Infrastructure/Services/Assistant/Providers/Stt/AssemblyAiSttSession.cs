// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-connection AssemblyAI v3 Universal-Streaming session that owns a WebSocket,
/// streams raw PCM16 binary frames, and parses Turn transcript messages.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

public sealed class AssemblyAiSttSession : ISttSession
{
    private readonly ClientWebSocket _ws;
    private readonly ILogger _logger;

    public AssemblyAiSttSession(ClientWebSocket ws, ILogger logger)
    {
        _ws = ws;
        _logger = logger;
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

        var result = AssemblyAiSttProvider.ParseResult(text);
        if (result == null)
            LogNonTurnFrame(text);
        return result;
    }

    private void LogNonTurnFrame(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var frameType = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (frameType == "Turn")
                return;
            if (frameType == "Begin")
                _logger.LogDebug("AssemblyAI streaming session opened: {Payload}", payload.ForLog());
            else if (frameType == "Termination")
                _logger.LogInformation("AssemblyAI streaming session terminated: {Payload}", payload.ForLog());
            else
                _logger.LogWarning("AssemblyAI streaming non-transcript frame (type={Type}): {Payload}", frameType.ForLog(), payload.ForLog());
        }
        catch (JsonException)
        {
            _logger.LogWarning("AssemblyAI streaming unparseable frame: {Payload}", payload.ForLog());
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_ws.State == WebSocketState.Open)
        {
            var terminate = Encoding.UTF8.GetBytes("{\"type\":\"Terminate\"}");
            await _ws.SendAsync(new ArraySegment<byte>(terminate), WebSocketMessageType.Text, true, CancellationToken.None);
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
        }
        _ws.Dispose();
    }
}
