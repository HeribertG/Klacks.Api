// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deepgram real-time STT provider factory.
/// Creates DeepgramSttSession instances that connect via WebSocket for audio streaming.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public class DeepgramSttProvider : ISttProvider
{
    public string ProviderId => SttProviderConstants.Deepgram;

    public async Task<ISttSession> CreateSessionAsync(SttConfig config, CancellationToken ct = default)
    {
        var ws = new ClientWebSocket();
        var url = $"{SttProviderConstants.DeepgramWssUrl}?model=nova-2&language={config.Language}&encoding=linear16&sample_rate={config.SampleRate}&interim_results=true&punctuate=true";
        ws.Options.SetRequestHeader("Authorization", $"Token {config.ApiKey}");
        await ws.ConnectAsync(new Uri(url), ct);
        return new DeepgramSttSession(ws);
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
