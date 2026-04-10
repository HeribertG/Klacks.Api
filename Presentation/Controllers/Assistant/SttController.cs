// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// WebSocket proxy for Speech-to-Text streaming.
/// Receives audio chunks from the browser, forwards to the configured STT provider,
/// and returns transcription results back to the browser.
/// </summary>
/// <param name="sttProviders">All registered ISttProvider implementations</param>
/// <param name="settingsRepository">Reads STT provider and API key from settings</param>
/// <param name="logger">Logger instance</param>
namespace Klacks.Api.Presentation.Controllers.Assistant;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/backend/assistant/stt")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SttController : ControllerBase
{
    private readonly IEnumerable<ISttProvider> _sttProviders;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<SttController> _logger;

    public SttController(
        IEnumerable<ISttProvider> sttProviders,
        ISettingsRepository settingsRepository,
        ILogger<SttController> logger)
    {
        _sttProviders = sttProviders;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = _sttProviders.Select(p => new { p.ProviderId }).ToList();
        providers.Add(new { ProviderId = SttProviderConstants.Browser });
        return Ok(providers);
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] SttTestRequest request)
    {
        var provider = _sttProviders.FirstOrDefault(p => p.ProviderId == request.ProviderId);
        if (provider == null)
            return BadRequest($"Unknown provider: {request.ProviderId}");

        try
        {
            var config = new SttConfig(request.ApiKey, "en");
            var ws = await provider.ConnectAsync(config);
            await provider.DisconnectAsync(ws);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, error = ex.Message });
        }
    }

    [Route("stream")]
    public async Task Stream()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        var browserWs = await HttpContext.WebSockets.AcceptWebSocketAsync();

        try
        {
            var providerSetting = await _settingsRepository.GetSetting(Settings.ASSISTANT_STT_PROVIDER);
            var providerId = providerSetting?.Value ?? SttProviderConstants.Browser;

            if (providerId == SttProviderConstants.Browser)
            {
                await browserWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Use browser STT", CancellationToken.None);
                return;
            }

            var provider = _sttProviders.FirstOrDefault(p => p.ProviderId == providerId);
            if (provider == null)
            {
                await SendError(browserWs, $"Unknown STT provider: {providerId}");
                return;
            }

            var apiKeySetting = await _settingsRepository.GetSetting(Settings.ASSISTANT_STT_API_KEY);
            if (string.IsNullOrWhiteSpace(apiKeySetting?.Value))
            {
                await SendError(browserWs, "No API key configured for STT provider");
                return;
            }

            var config = new SttConfig(apiKeySetting.Value, "de");
            var providerWs = await provider.ConnectAsync(config);

            var cts = new CancellationTokenSource();

            var forwardTask = ForwardAudioToProvider(browserWs, provider, providerWs, cts.Token);
            var receiveTask = ForwardResultsToBrowser(browserWs, provider, providerWs, cts.Token);

            await Task.WhenAny(forwardTask, receiveTask);
            cts.Cancel();

            await provider.DisconnectAsync(providerWs);
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "WebSocket connection closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STT streaming error");
        }
    }

    private static async Task ForwardAudioToProvider(WebSocket browserWs, ISttProvider provider, WebSocket providerWs, CancellationToken ct)
    {
        var buffer = new byte[8192];
        while (!ct.IsCancellationRequested && browserWs.State == WebSocketState.Open)
        {
            var result = await browserWs.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var chunk = new byte[result.Count];
            Array.Copy(buffer, chunk, result.Count);
            await provider.SendAudioAsync(providerWs, chunk, ct);
        }
    }

    private static async Task ForwardResultsToBrowser(WebSocket browserWs, ISttProvider provider, WebSocket providerWs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && browserWs.State == WebSocketState.Open)
        {
            var sttResult = await provider.ReceiveAsync(providerWs, ct);
            if (sttResult == null)
                continue;

            var json = JsonSerializer.Serialize(new { text = sttResult.Text, isFinal = sttResult.IsFinal, confidence = sttResult.Confidence });
            var bytes = Encoding.UTF8.GetBytes(json);
            await browserWs.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }
    }

    private static async Task SendError(WebSocket ws, string message)
    {
        var json = JsonSerializer.Serialize(new { error = message });
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, message, CancellationToken.None);
    }
}

public record SttTestRequest(string ProviderId, string ApiKey);
