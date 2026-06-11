// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for text-to-speech synthesis endpoints.
/// Routes synthesis requests to the appropriate ITtsProvider based on ProviderId.
/// </summary>
/// <param name="ttsProviders">All registered ITtsProvider implementations</param>
/// <param name="logger">Logger for diagnostics and error tracking</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Presentation.DTOs.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/tts")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TtsController : ControllerBase
{
    private readonly ILogger<TtsController> _logger;
    private readonly IEnumerable<ITtsProvider> _ttsProviders;

    private const string TestText = "Test.";

    public TtsController(
        ILogger<TtsController> logger,
        IEnumerable<ITtsProvider> ttsProviders)
    {
        _logger = logger;
        _ttsProviders = ttsProviders;
    }

    [HttpGet("voices")]
    public async Task<IActionResult> GetVoices([FromQuery] string? providerId = null, CancellationToken ct = default)
    {
        var effectiveProviderId = providerId ?? TtsProviderConstants.Edge;
        var provider = _ttsProviders.FirstOrDefault(p => p.ProviderId == effectiveProviderId);
        if (provider == null)
            return BadRequest($"Unknown TTS provider: {effectiveProviderId}");

        var voices = await provider.GetVoicesAsync(ct);
        return Ok(voices);
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] TtsTestRequest request, CancellationToken ct)
    {
        var provider = _ttsProviders.FirstOrDefault(p => p.ProviderId == request.ProviderId);
        if (provider == null)
        {
            return BadRequest(new TtsTestResult(false, $"Unknown provider: {request.ProviderId}"));
        }

        try
        {
            var audio = await provider.SynthesizeAsync(TestText, TtsProviderConstants.AutoVoice, TtsProviderConstants.DefaultLocale, ct);
            return Ok(audio is { Length: > 0 }
                ? new TtsTestResult(true, null)
                : new TtsTestResult(false, "No audio returned"));
        }
        catch (Exception ex)
        {
            return Ok(new TtsTestResult(false, ex.Message));
        }
    }

    [HttpPost("synthesize")]
    public async Task<IActionResult> Synthesize([FromBody] TtsSynthesizeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text cannot be empty");
        }

        if (request.Text.Length > TtsProviderConstants.MaxSynthesisTextLength)
        {
            return BadRequest($"Text exceeds maximum length of {TtsProviderConstants.MaxSynthesisTextLength} characters");
        }

        var providerId = request.ProviderId ?? TtsProviderConstants.Edge;
        var provider = _ttsProviders.FirstOrDefault(p => p.ProviderId == providerId);
        if (provider == null)
        {
            return BadRequest($"Unknown TTS provider: {providerId}");
        }

        try
        {
            var voiceId = request.VoiceId ?? TtsProviderConstants.AutoVoice;
            var locale = request.Locale ?? TtsProviderConstants.DefaultLocale;
            var spokenText = TtsTextSanitizer.Sanitize(request.Text);
            if (string.IsNullOrWhiteSpace(spokenText))
            {
                return BadRequest("Text cannot be empty");
            }

            var chunks = TtsTextChunker.Split(spokenText, TtsProviderConstants.SynthesisChunkLength);

            using var audioStream = new MemoryStream();
            foreach (var chunk in chunks)
            {
                if (string.IsNullOrWhiteSpace(chunk))
                {
                    continue;
                }

                var chunkAudio = await provider.SynthesizeAsync(chunk, voiceId, locale, ct);
                audioStream.Write(chunkAudio, 0, chunkAudio.Length);
            }

            return File(audioStream.ToArray(), "audio/mpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS synthesis failed");
            return StatusCode(500, "Text-to-speech synthesis failed");
        }
    }
}
