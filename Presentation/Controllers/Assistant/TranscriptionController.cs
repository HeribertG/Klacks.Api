// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for transcription enhancement endpoints.
/// Accepts raw speech-to-text output and returns LLM-cleaned text.
/// </summary>
/// <param name="transcriptionEnhancerService">Service that performs the LLM-based transcription cleanup</param>
/// <param name="logger">Logger for diagnostic output</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Presentation.DTOs.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/transcription")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TranscriptionController : ControllerBase
{
    private readonly ITranscriptionEnhancerService _transcriptionEnhancerService;
    private readonly ILogger<TranscriptionController> _logger;

    public TranscriptionController(
        ITranscriptionEnhancerService transcriptionEnhancerService,
        ILogger<TranscriptionController> logger)
    {
        _transcriptionEnhancerService = transcriptionEnhancerService;
        _logger = logger;
    }

    [HttpPost("enhance")]
    public async Task<ActionResult<TranscriptionEnhanceResponse>> Enhance(
        [FromBody] TranscriptionEnhanceRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest("Raw text cannot be empty");
        }

        _logger.LogInformation("Transcription enhancement requested for locale {Locale}", request.Locale);

        var enhancedText = await _transcriptionEnhancerService.EnhanceTranscriptionAsync(
            request.RawText, request.Locale, request.ModelId, ct);

        return Ok(new TranscriptionEnhanceResponse { EnhancedText = enhancedText });
    }
}
