// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// AssemblyAI real-time STT provider factory.
/// Creates AssemblyAiSttSession instances that connect via WebSocket for audio streaming.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public class AssemblyAiSttProvider : ISttProvider
{
    public string ProviderId => SttProviderConstants.AssemblyAi;

    public async Task<ISttSession> CreateSessionAsync(SttConfig config, CancellationToken ct = default)
    {
        var ws = new ClientWebSocket();
        var url = $"{SttProviderConstants.AssemblyAiWssUrl}?sample_rate={config.SampleRate}&token={config.ApiKey}";
        await ws.ConnectAsync(new Uri(url), ct);
        return new AssemblyAiSttSession(ws);
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
