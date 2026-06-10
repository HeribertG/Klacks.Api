// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports whether the DeepL translation service is configured (API key present) via
/// GetTranslationStatusQuery, together with the core target languages. Point the user to
/// update_deepl_settings when the service is not configured.
/// </summary>

using Klacks.Api.Application.Queries.Settings.Languages;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_translation_status")]
public class GetTranslationStatusSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetTranslationStatusSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var configured = await _mediator.Send(new GetTranslationStatusQuery(), cancellationToken);

        var targetLanguages = LanguageConfig.SupportedLanguages;

        var message = configured
            ? $"DeepL translation service is configured. Texts can be translated automatically into: {string.Join(", ", targetLanguages)}."
            : "DeepL translation service is not configured. Set the DeepL API key via update_deepl_settings to enable automatic translations.";

        return SkillResult.SuccessResult(
            new { Configured = configured, TargetLanguages = targetLanguages },
            message);
    }
}
