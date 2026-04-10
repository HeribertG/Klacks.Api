// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for text-to-speech synthesis endpoints.
/// </summary>
/// <param name="text">The text to convert to speech</param>
/// <param name="locale">The locale code for voice selection</param>
using Klacks.Api.Domain.Interfaces.Assistant;
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
    private readonly ITextToSpeechService _textToSpeechService;

    private const int MaxTextLength = 5000;

    public TtsController(
        ILogger<TtsController> logger,
        ITextToSpeechService textToSpeechService)
    {
        _logger = logger;
        _textToSpeechService = textToSpeechService;
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

        try
        {
            var voiceId = request.VoiceId ?? "auto";
            var audioBytes = await _textToSpeechService.SynthesizeAsync(request.Text, voiceId, request.Locale ?? "en", ct);
            return File(audioBytes, "audio/mpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS synthesis failed");
            return StatusCode(500, "Text-to-speech synthesis failed");
        }
    }

    public record TtsSynthesizeRequest(string Text, string? Locale, string? ProviderId = null, string? VoiceId = null);
}
