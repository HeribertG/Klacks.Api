// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs (activates) a locally available language pack via InstallLanguagePluginCommand after
/// validating the code against ListLanguagePluginsQuery. Core languages are always active and
/// cannot be installed; use list_languages to discover available pack codes.
/// </summary>
/// <param name="code">Required. Language pack code, e.g. 'gsw' or 'rm'.</param>

using Klacks.Api.Application.Commands.Settings.Languages;
using Klacks.Api.Application.Queries.Settings.Languages;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("install_language_pack")]
public class InstallLanguagePackSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public InstallLanguagePackSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var code = GetParameter<string>(parameters, "code");
        if (string.IsNullOrWhiteSpace(code))
        {
            return SkillResult.Error("Missing required parameter 'code'. Use list_languages to see available language packs.");
        }

        var normalized = code.Trim().ToLowerInvariant();

        var packs = await _mediator.Send(new ListLanguagePluginsQuery(), cancellationToken);
        var pack = packs.FirstOrDefault(p => string.Equals(p.Code, normalized, StringComparison.OrdinalIgnoreCase));

        if (pack == null)
        {
            return SkillResult.Error($"Language pack '{normalized}' is unknown. Use list_languages to see available packs.");
        }

        if (pack.IsCore)
        {
            return SkillResult.Error($"'{normalized}' is a core language and is always active; it cannot be installed as a pack.");
        }

        if (pack.IsInstalled)
        {
            return SkillResult.SuccessResult(
                new { pack.Code, pack.DisplayName, IsInstalled = true },
                $"Language pack '{pack.DisplayName}' ({pack.Code}) is already installed.");
        }

        var installed = await _mediator.Send(new InstallLanguagePluginCommand(pack.Code), cancellationToken);
        if (!installed)
        {
            return SkillResult.Error($"Language pack '{pack.Code}' could not be installed. It may require a newer Klacks version (minimum {pack.MinKlacksVersion}).");
        }

        return SkillResult.SuccessResult(
            new { pack.Code, pack.DisplayName, IsInstalled = true, pack.TranslationCount },
            $"Language pack '{pack.DisplayName}' ({pack.Code}) installed and activated: translations, synonyms and geo data are now available. Use uninstall_language_pack to deactivate it again.");
    }
}
