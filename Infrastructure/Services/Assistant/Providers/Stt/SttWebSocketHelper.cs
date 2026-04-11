// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Helper for reading complete (potentially multi-frame) WebSocket messages.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text;

internal static class SttWebSocketHelper
{
    internal static async Task<(WebSocketMessageType Type, string Text)> ReceiveFullMessageAsync(WebSocket ws, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[4096];
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                return (WebSocketMessageType.Close, string.Empty);
            await ms.WriteAsync(buffer, 0, result.Count, ct);
        } while (!result.EndOfMessage);

        ms.Position = 0;
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(ct);
        return (result.MessageType, text);
    }
}
