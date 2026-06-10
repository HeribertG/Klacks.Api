// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing calculation macro (script) in the settings. Loads the macro by id via
/// <see cref="Klacks.Api.Application.Queries.Settings.Macros.GetQuery"/>, merges only the provided
/// fields onto the current values and persists the result via
/// <see cref="Klacks.Api.Application.Commands.Settings.Macros.PutCommand"/>.
/// </summary>
/// <param name="macroId">Required. The id of the macro to update.</param>
/// <param name="name">Optional. New macro name.</param>
/// <param name="script">Optional. New macro script body (stored as content).</param>
/// <param name="description">Optional. New description applied to all core languages.</param>

using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_macro")]
public class UpdateMacroSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateMacroSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rawId = GetParameter<string>(parameters, "macroId");
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return SkillResult.Error("macroId is required.");
        }

        if (!Guid.TryParse(rawId, out var macroId))
        {
            return SkillResult.Error($"'{rawId}' is not a valid macro id.");
        }

        var name = GetParameter<string>(parameters, "name");
        var script = GetParameter<string>(parameters, "script")
                     ?? GetParameter<string>(parameters, "content");
        var description = GetParameter<string>(parameters, "description");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(script)
            && string.IsNullOrWhiteSpace(description))
        {
            return SkillResult.Error("Nothing to update. Provide at least one of name, script, or description.");
        }

        var notFoundMessage = $"No macro found with id '{macroId}'.";

        MacroResource? existing;
        try
        {
            existing = await _mediator.Send(new GetQuery(macroId), cancellationToken);
        }
        catch (InvalidRequestException)
        {
            return SkillResult.Error(notFoundMessage);
        }

        if (existing == null)
        {
            return SkillResult.Error(notFoundMessage);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            existing.Name = name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(script))
        {
            existing.Content = script;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            var mlDescription = existing.Description ?? new MultiLanguage();
            foreach (var language in MultiLanguage.CoreLanguages)
            {
                mlDescription.SetValue(language, description);
            }

            existing.Description = mlDescription;
        }

        var updated = await _mediator.Send(new PutCommand(existing), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Macro '{existing.Name}' could not be updated.");
        }

        return SkillResult.SuccessResult(
            new { updated.Id, updated.Name },
            $"Macro '{updated.Name}' updated (id {updated.Id}).");
    }
}
