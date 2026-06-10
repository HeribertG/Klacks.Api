// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the active UI languages (including fallback order) and all known language packs with
/// their installation state via GetLanguagesQuery and ListLanguagePluginsQuery. Use this to find
/// pack codes for install_language_pack / uninstall_language_pack.
/// </summary>

using Klacks.Api.Application.Queries.Settings.Languages;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_languages")]
public class ListLanguagesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListLanguagesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var config = await _mediator.Send(new GetLanguagesQuery(), cancellationToken);
        var packs = await _mediator.Send(new ListLanguagePluginsQuery(), cancellationToken);

        var projected = packs
            .Select(p => new
            {
                p.Code,
                p.DisplayName,
                p.IsCore,
                p.IsInstalled,
                p.Version,
                p.Coverage,
                p.TranslationCount,
                p.Direction
            })
            .ToList();

        var installedOptional = projected.Count(p => !p.IsCore && p.IsInstalled);
        var totalOptional = projected.Count(p => !p.IsCore);

        return SkillResult.SuccessResult(
            new
            {
                ActiveLanguages = config.SupportedLanguages,
                config.FallbackOrder,
                LanguagePacks = projected
            },
            $"Active languages: {string.Join(", ", config.SupportedLanguages)}. " +
            $"{installedOptional} of {totalOptional} optional language packs installed.");
    }
}
