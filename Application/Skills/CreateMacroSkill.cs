// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a calculation macro (script) in the settings. Thin wrapper around
/// <see cref="Klacks.Api.Application.Commands.Settings.Macros.PostCommand"/>; the macro script
/// is stored as the macro content and can afterwards be referenced by name.
/// </summary>
/// <param name="name">Required. The macro name.</param>
/// <param name="script">Required. The macro script body (stored as content).</param>
/// <param name="description">Optional. A short description applied to all core languages.</param>

using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_macro")]
public class CreateMacroSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateMacroSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("name is required.");
        }

        var script = GetParameter<string>(parameters, "script");
        if (string.IsNullOrWhiteSpace(script))
        {
            return SkillResult.Error("script is required.");
        }

        var description = GetParameter<string>(parameters, "description");

        var resource = new MacroResource
        {
            Name = name.Trim(),
            Content = script,
            Type = (int)MacroTypeEnum.DefaultResult,
            Description = BuildDescription(description)
        };

        var created = await _mediator.Send(new PostCommand(resource), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error($"Macro '{name.Trim()}' could not be created.");
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.Name },
            $"Macro '{created.Name}' created (id {created.Id}).");
    }

    private static MultiLanguage BuildDescription(string? description)
    {
        var value = new MultiLanguage();
        if (string.IsNullOrWhiteSpace(description))
        {
            return value;
        }

        foreach (var language in MultiLanguage.CoreLanguages)
        {
            value.SetValue(language, description);
        }

        return value;
    }
}
