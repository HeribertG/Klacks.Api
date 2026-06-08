// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// AssemblyAI v3 Universal-Streaming (u3-rt-pro) STT provider factory.
/// Creates AssemblyAiSttSession instances that connect via WebSocket and stream raw PCM16 audio.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Net.WebSockets;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

public class AssemblyAiSttProvider : ISttProvider
{
    private readonly ILogger<AssemblyAiSttProvider> _logger;

    public AssemblyAiSttProvider(ILogger<AssemblyAiSttProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderId => SttProviderConstants.AssemblyAi;

    public async Task<ISttSession> CreateSessionAsync(SttConfig config, CancellationToken ct = default)
    {
        var ws = new ClientWebSocket();
        var url = $"{SttProviderConstants.AssemblyAiWssUrl}?speech_model={SttProviderConstants.AssemblyAiSpeechModel}&encoding={SttProviderConstants.AssemblyAiEncoding}&sample_rate={config.SampleRate}";
        ws.Options.SetRequestHeader("Authorization", config.ApiKey);
        await ws.ConnectAsync(new Uri(url), ct);
        return new AssemblyAiSttSession(ws, _logger);
    }

    public async Task ValidateAsync(SttConfig config, CancellationToken ct = default)
    {
        await using var session = await CreateSessionAsync(config, ct);
    }

    internal static SttResult? ParseResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "Turn")
            return null;

        if (!root.TryGetProperty("transcript", out var transcriptProp))
            return null;

        var text = transcriptProp.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var isFinal = root.TryGetProperty("end_of_turn", out var eot) && eot.GetBoolean();
        var confidence = root.TryGetProperty("end_of_turn_confidence", out var conf) ? conf.GetSingle() : 1.0f;

        return new SttResult(text, isFinal, confidence);
    }
}
