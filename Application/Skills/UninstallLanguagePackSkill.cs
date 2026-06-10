// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Uninstalls (deactivates) an installed language pack via UninstallLanguagePluginCommand after
/// validating the code against ListLanguagePluginsQuery. Core languages cannot be removed; the
/// pack files stay on disk, so install_language_pack can re-activate the pack at any time.
/// </summary>
/// <param name="code">Required. Language pack code, e.g. 'gsw' or 'rm'.</param>

using Klacks.Api.Application.Commands.Settings.Languages;
using Klacks.Api.Application.Queries.Settings.Languages;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("uninstall_language_pack")]
public class UninstallLanguagePackSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UninstallLanguagePackSkill(IMediator mediator)
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
            return SkillResult.Error("Missing required parameter 'code'. Use list_languages to see installed language packs.");
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
            return SkillResult.Error($"'{normalized}' is a core language and cannot be uninstalled.");
        }

        if (!pack.IsInstalled)
        {
            return SkillResult.Error($"Language pack '{pack.Code}' is not installed.");
        }

        var uninstalled = await _mediator.Send(new UninstallLanguagePluginCommand(pack.Code), cancellationToken);
        if (!uninstalled)
        {
            return SkillResult.Error($"Language pack '{pack.Code}' could not be uninstalled.");
        }

        return SkillResult.SuccessResult(
            new { pack.Code, pack.DisplayName, IsInstalled = false },
            $"Language pack '{pack.DisplayName}' ({pack.Code}) uninstalled. The pack files remain on disk and can be re-activated via install_language_pack.");
    }
}
