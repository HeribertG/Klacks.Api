// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for text-to-speech synthesis endpoints.
/// Routes synthesis requests to the appropriate ITtsProvider based on ProviderId.
/// </summary>
/// <param name="ttsProviders">All registered ITtsProvider implementations</param>
/// <param name="logger">Logger for diagnostics and error tracking</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
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

    private const int MaxTextLength = 5000;

    public TtsController(
        ILogger<TtsController> logger,
        IEnumerable<ITtsProvider> ttsProviders)
    {
        _logger = logger;
        _ttsProviders = ttsProviders;
    }

    [HttpPost("synthesize")]
    public async Task<IActionResult> Synthesize([FromBody] TtsSynthesizeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text cannot be empty");
        }

        if (request.Text.Length > MaxTextLength)
        {
            return BadRequest($"Text exceeds maximum length of {MaxTextLength} characters");
        }

        var providerId = request.ProviderId ?? TtsProviderConstants.Edge;
        var provider = _ttsProviders.FirstOrDefault(p => p.ProviderId == providerId);
        if (provider == null)
        {
            return BadRequest($"Unknown TTS provider: {providerId}");
        }

        try
        {
            var voiceId = request.VoiceId ?? "auto";
            var locale = request.Locale ?? "en";
            var audioBytes = await provider.SynthesizeAsync(request.Text, voiceId, locale, ct);
            return File(audioBytes, "audio/mpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS synthesis failed");
            return StatusCode(500, "Text-to-speech synthesis failed");
        }
    }
}
