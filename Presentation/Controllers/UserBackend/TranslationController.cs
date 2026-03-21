// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for text translation via DeepL API.
/// </summary>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Translation;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class TranslationController : BaseController
{
    private readonly ITranslationService _translationService;

    public TranslationController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<bool>> GetStatus()
    {
        return Ok(await _translationService.IsConfiguredAsync());
    }

    [HttpPost("translate-all")]
    public async Task<ActionResult<TranslationResponse>> TranslateToAll([FromBody] TranslationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text is required");
        }

        if (string.IsNullOrWhiteSpace(request.SourceLanguage))
        {
            return BadRequest("Source language is required");
        }

        if (!LanguageConfig.SupportedLanguages.Contains(request.SourceLanguage, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Unsupported source language: {request.SourceLanguage}");
        }

        if (!await _translationService.IsConfiguredAsync())
        {
            return BadRequest("Translation service is not configured. Please set the DeepL API key in settings.");
        }

        var translations = await _translationService.TranslateToAllLanguagesAsync(
            request.Text,
            request.SourceLanguage);

        var response = new TranslationResponse
        {
            De = translations.GetValueOrDefault("de", request.Text),
            En = translations.GetValueOrDefault("en", request.Text),
            Fr = translations.GetValueOrDefault("fr", request.Text),
            It = translations.GetValueOrDefault("it", request.Text)
        };

        return Ok(response);
    }
}
