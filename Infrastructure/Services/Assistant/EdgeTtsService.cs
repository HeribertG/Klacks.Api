// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Text-to-speech service using Microsoft Edge's online TTS via WebSocket.
/// </summary>
/// <param name="_logger">Logger for diagnostics and error tracking</param>
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class EdgeTtsService : ITtsProvider
{
    private readonly ILogger<EdgeTtsService> _logger;

    private const int MaxTextLength = 5000;
    private const string WssBaseUrl = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1";
    private const string OutputFormat = "audio-24khz-48kbitrate-mono-mp3";
    private const string ChromiumVersion = "143.0.0.0";
    private const string EdgeVersion = "143.0.0.0";
    private const string SecMsGecVersion = "2-143.0.3650.75";
    private const int MuidByteLength = 16;
    private const int MaxClockSkewRetries = 1;
    private const string FallbackVoiceShortName = "en-US-GuyNeural";
    private const string FallbackSsmlLang = "en-US";

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
        ["cs"] = "cs-CZ-AntoninNeural",
        ["da"] = "da-DK-JeppeNeural",
        ["el"] = "el-GR-NestorasNeural",
        ["fi"] = "fi-FI-HarriNeural",
        ["nb"] = "nb-NO-FinnNeural",
        ["nl"] = "nl-NL-MaartenNeural",
        ["ro"] = "ro-RO-EmilNeural",
        ["sv"] = "sv-SE-MattiasNeural",
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

        using var ws = await ConnectAsync(connectionId, ct);

        await SendSpeechConfig(ws, ct);
        await SendSsml(ws, connectionId, voice, text, ct);

        return await ReceiveAudio(ws, ct);
    }

    private async Task<ClientWebSocket> ConnectAsync(string connectionId, CancellationToken ct)
    {
        var clockSkewSeconds = 0L;
        for (var attempt = 0; ; attempt++)
        {
            var ws = CreateWebSocket();
            try
            {
                var secMsGec = EdgeTtsSecMsGecGenerator.Generate(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + clockSkewSeconds);
                var url = $"{WssBaseUrl}?TrustedClientToken={EdgeTtsSecMsGecGenerator.TrustedClientToken}&ConnectionId={connectionId}&Sec-MS-GEC={secMsGec}&Sec-MS-GEC-Version={SecMsGecVersion}";
                await ws.ConnectAsync(new Uri(url), ct);
                return ws;
            }
            catch (WebSocketException) when (ws.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                var serverTime = ws.HttpResponseHeaders is not null
                    && ws.HttpResponseHeaders.TryGetValue("Date", out var dateValues)
                    && DateTimeOffset.TryParse(dateValues.FirstOrDefault(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                        ? parsed
                        : (DateTimeOffset?)null;
                ws.Dispose();

                if (attempt < MaxClockSkewRetries && serverTime is not null)
                {
                    clockSkewSeconds = serverTime.Value.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    _logger.LogWarning("Edge TTS returned HTTP 403, retrying with server clock offset of {Skew}s", clockSkewSeconds);
                    continue;
                }

                _logger.LogError("Edge TTS rejected the Sec-MS-GEC token (HTTP 403, clock offset {Skew}s). Microsoft may have changed the DRM scheme; compare with edge-tts drm.py", clockSkewSeconds);
                throw;
            }
            catch
            {
                ws.Dispose();
                throw;
            }
        }
    }

    private static ClientWebSocket CreateWebSocket()
    {
        var ws = new ClientWebSocket();
        ws.Options.CollectHttpResponseDetails = true;
        ws.Options.SetRequestHeader("Pragma", "no-cache");
        ws.Options.SetRequestHeader("Cache-Control", "no-cache");
        ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
        ws.Options.SetRequestHeader("User-Agent",
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{ChromiumVersion} Safari/537.36 Edg/{EdgeVersion}");
        ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br, zstd");
        ws.Options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");
        ws.Options.SetRequestHeader("Cookie", $"muid={Convert.ToHexString(RandomNumberGenerator.GetBytes(MuidByteLength))};");
        return ws;
    }

    private static string ResolveVoice(string locale)
    {
        if (VoiceMap.TryGetValue(locale, out var voice))
        {
            return voice;
        }

        var langPrefix = LanguageTag.BaseLanguage(locale) ?? locale;
        return VoiceMap.GetValueOrDefault(langPrefix, FallbackVoiceShortName);
    }

    /// <summary>
    /// Derives the SSML xml:lang from the neural voice short name (e.g. de-DE-ConradNeural → de-DE),
    /// so sentence melody matches the spoken language instead of a hardcoded locale.
    /// </summary>
    /// <param name="voiceShortName">Edge neural voice short name</param>
    private static string ResolveSsmlLang(string voiceShortName)
    {
        var parts = voiceShortName.Split('-');
        return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : FallbackSsmlLang;
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
        var ssmlLang = ResolveSsmlLang(voice);
        var ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{ssmlLang}'>" +
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
