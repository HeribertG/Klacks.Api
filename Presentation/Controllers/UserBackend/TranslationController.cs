// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    public ActionResult<bool> GetStatus()
    {
        return Ok(_translationService.IsConfigured);
    }

    [HttpPost("translate-all")]
    public async Task<ActionResult<TranslationResponse>> TranslateToAll([FromBody] TranslationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text is required");
        }

        if (!_translationService.IsConfigured)
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
