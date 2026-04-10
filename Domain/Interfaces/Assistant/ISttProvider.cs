// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Abstraction for Speech-to-Text providers that stream audio and return transcription results.
/// </summary>
/// <param name="ProviderId">Unique provider identifier (e.g. "deepgram", "groq-whisper")</param>
namespace Klacks.Api.Domain.Interfaces.Assistant;

using System.Net.WebSockets;
using Klacks.Api.Domain.Models.Assistant;

public interface ISttProvider
{
    string ProviderId { get; }
    Task<WebSocket> ConnectAsync(SttConfig config, CancellationToken ct = default);
    Task SendAudioAsync(WebSocket ws, byte[] audioChunk, CancellationToken ct = default);
    Task<SttResult?> ReceiveAsync(WebSocket ws, CancellationToken ct = default);
    Task DisconnectAsync(WebSocket ws, CancellationToken ct = default);
}
