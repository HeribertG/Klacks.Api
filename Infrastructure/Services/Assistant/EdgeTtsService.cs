// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Text-to-speech service using Microsoft Edge's online TTS via WebSocket.
/// </summary>
/// <param name="_logger">Logger for diagnostics and error tracking</param>
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class EdgeTtsService : ITtsProvider
{
    private readonly ILogger<EdgeTtsService> _logger;

    private const int MaxTextLength = 5000;
    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    private const string WssBaseUrl = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1";
    private const string OutputFormat = "audio-24khz-48kbitrate-mono-mp3";
    private const string ChromiumVersion = "143.0.0.0";
    private const string EdgeVersion = "143.0.0.0";
    private const string SecMsGecVersion = "1-143.0.3650.75";
    private const long WindowsEpochOffset = 11644473600L;
    private const string FallbackVoiceShortName = "en-US-GuyNeural";

    private static readonly Dictionary<string, string> VoiceMap = new()
    {
        ["de"] = "de-DE-ConradNeural",
        ["en"] = "en-US-GuyNeural",
        ["fr"] = "fr-FR-HenriNeural",
        ["es"] = "es-ES-AlvaroNeural",
        ["pt"] = "pt-BR-AntonioNeural",
        ["it"] = "it-IT-DiegoNeural",
        ["tr"] = "tr-TR-AhmetNeural",
        ["pl"] = "pl-PL-MarekNeural",
        ["ja"] = "ja-JP-KeitaNeural",
        ["he"] = "he-IL-AvriNeural",
        ["zh-TW"] = "zh-TW-YunJheNeural",
        ["vi"] = "vi-VN-NamMinhNeural",
        ["ko"] = "ko-KR-InJoonNeural",
        ["zh-CN"] = "zh-CN-YunxiNeural",
        ["th"] = "th-TH-NiwatNeural",
        ["id"] = "id-ID-ArdiNeural",
        ["ms"] = "ms-MY-OsmanNeural",
        ["ar"] = "ar-SA-HamedNeural",
    };

    public string ProviderId => TtsProviderConstants.Edge;

    public EdgeTtsService(ILogger<EdgeTtsService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<TtsVoice>> GetVoicesAsync(CancellationToken ct = default)
    {
        var voices = VoiceMap.Select(kvp => new TtsVoice(
            VoiceId: kvp.Value,
            Locale: kvp.Key,
            DisplayName: kvp.Value.Replace("Neural", "").Trim()
        )).ToList();
        return Task.FromResult<IReadOnlyList<TtsVoice>>(voices);
    }

    public async Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default)
    {
        var truncatedText = text.Length > MaxTextLength ? text[..MaxTextLength] : text;
        var voiceShortName = (string.IsNullOrWhiteSpace(voiceId) || voiceId == TtsProviderConstants.AutoVoice)
            ? ResolveVoice(locale)
            : voiceId;

        return await SynthesizeInternalAsync(truncatedText, voiceShortName, ct);
    }

    private async Task<byte[]> SynthesizeInternalAsync(string text, string voice, CancellationToken ct)
    {
        var connectionId = Guid.NewGuid().ToString("N");

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Pragma", "no-cache");
        ws.Options.SetRequestHeader("Cache-Control", "no-cache");
        ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
        ws.Options.SetRequestHeader("User-Agent",
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{ChromiumVersion} Safari/537.36 Edg/{EdgeVersion}");
        ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br, zstd");
        ws.Options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");

        var secMsGec = GenerateSecMsGec();
        var url = $"{WssBaseUrl}?TrustedClientToken={TrustedClientToken}&ConnectionId={connectionId}&Sec-MS-GEC={secMsGec}&Sec-MS-GEC-Version={SecMsGecVersion}";

        await ws.ConnectAsync(new Uri(url), ct);

        await SendSpeechConfig(ws, ct);
        await SendSsml(ws, connectionId, voice, text, ct);

        return await ReceiveAudio(ws, ct);
    }

    private static string ResolveVoice(string locale)
    {
        if (VoiceMap.TryGetValue(locale, out var voice))
        {
            return voice;
        }

        var langPrefix = locale.Contains('-') ? locale[..locale.IndexOf('-')] : locale;
        return VoiceMap.GetValueOrDefault(langPrefix, FallbackVoiceShortName);
    }

    private static string GenerateSecMsGec()
    {
        var unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowsTicks = (unixSeconds + WindowsEpochOffset);
        windowsTicks -= windowsTicks % 300;
        var scaledTicks = (long)(windowsTicks * 1e9 / 100);

        var input = $"{scaledTicks}{TrustedClientToken}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }

    private static async Task SendSpeechConfig(ClientWebSocket ws, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var message = $"X-Timestamp:{timestamp}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n" +
                      $"{{\"context\":{{\"synthesis\":{{\"audio\":{{\"metadataoptions\":{{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"}},\"outputFormat\":\"{OutputFormat}\"}}}}}}}}";

        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private static async Task SendSsml(ClientWebSocket ws, string requestId, string voice, string text, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var escapedText = System.Security.SecurityElement.Escape(text);
        var ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
                   $"<voice name='{voice}'><prosody pitch='+0Hz' rate='+0%' volume='+0%'>{escapedText}</prosody></voice></speak>";

        var message = $"X-RequestId:{requestId}\r\nContent-Type:application/ssml+xml\r\nX-Timestamp:{timestamp}Z\r\nPath:ssml\r\n\r\n{ssml}";

        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task<byte[]> ReceiveAudio(ClientWebSocket ws, CancellationToken ct)
    {
        using var audioStream = new MemoryStream();
        var buffer = new byte[8192];

        while (true)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var fullMessage = await ReadFullTextMessage(ws, buffer, result, ct);
                if (fullMessage.Contains("Path:turn.end"))
                {
                    break;
                }
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                var binaryData = await ReadFullBinaryMessage(ws, buffer, result, ct);
                ExtractAudioFromBinary(binaryData, audioStream);
            }
        }

        if (audioStream.Length == 0)
        {
            _logger.LogWarning("TTS synthesis returned empty audio");
        }

        return audioStream.ToArray();
    }

    private static async Task<string> ReadFullTextMessage(ClientWebSocket ws, byte[] buffer, WebSocketReceiveResult firstResult, CancellationToken ct)
    {
        if (firstResult.EndOfMessage)
        {
            return Encoding.UTF8.GetString(buffer, 0, firstResult.Count);
        }

        using var ms = new MemoryStream();
        ms.Write(buffer, 0, firstResult.Count);
        var result = firstResult;
        while (!result.EndOfMessage)
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            ms.Write(buffer, 0, result.Count);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static async Task<byte[]> ReadFullBinaryMessage(ClientWebSocket ws, byte[] buffer, WebSocketReceiveResult firstResult, CancellationToken ct)
    {
        if (firstResult.EndOfMessage)
        {
            return buffer.AsSpan(0, firstResult.Count).ToArray();
        }

        using var ms = new MemoryStream();
        ms.Write(buffer, 0, firstResult.Count);
        var result = firstResult;
        while (!result.EndOfMessage)
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            ms.Write(buffer, 0, result.Count);
        }
        return ms.ToArray();
    }

    private static void ExtractAudioFromBinary(byte[] data, MemoryStream audioStream)
    {
        if (data.Length < 2)
        {
            return;
        }

        var headerLength = (data[0] << 8) | data[1];
        var audioOffset = 2 + headerLength;
        if (audioOffset < data.Length)
        {
            audioStream.Write(data, audioOffset, data.Length - audioOffset);
        }
    }
}
