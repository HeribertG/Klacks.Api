// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing calculation macro (script) in the settings. The macro is identified by
/// macroId (preferred) or by name, resolved fuzzily and unambiguously via <see cref="MacroResolver"/>
/// over <see cref="Klacks.Api.Application.Queries.Settings.Macros.ListQuery"/>. Loads the macro via
/// <see cref="Klacks.Api.Application.Queries.Settings.Macros.GetQuery"/>, merges only the provided
/// fields onto the current values and persists the result via
/// <see cref="Klacks.Api.Application.Commands.Settings.Macros.PutCommand"/>. When the script of a
/// macro carrying a standard function is changed, the success message warns that the macro now
/// counts as customer-owned and future automatic region-setup updates will fail with a conflict.
/// </summary>
/// <param name="macroId">Optional. The id of the macro to update. Preferred over macroName.</param>
/// <param name="macroName">Optional. The macro name used to resolve the macro when macroId is omitted.</param>
/// <param name="name">Optional. New macro name.</param>
/// <param name="script">Optional. New macro script body (stored as content).</param>
/// <param name="description">Optional. New description applied to all core languages.</param>

using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_macro")]
public class UpdateMacroSkill : BaseSkillImplementation
{
    private const string StandardMacroEditedHint =
        "Note: this macro carries a standard function and is normally kept in sync automatically by the "
        + "region setup. After this manual script change it counts as customer-owned, and future automatic "
        + "updates of this macro will fail with a conflict. Please point this out to the user.";

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
        var name = GetParameter<string>(parameters, "name");
        var script = GetParameter<string>(parameters, "script")
                     ?? GetParameter<string>(parameters, "content");
        var description = GetParameter<string>(parameters, "description");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(script)
            && string.IsNullOrWhiteSpace(description))
        {
            return SkillResult.Error("Nothing to update. Provide at least one of name, script, or description.");
        }

        var (resolvedId, resolveError) = await ResolveMacroIdAsync(parameters, cancellationToken);
        if (resolvedId == null)
        {
            return SkillResult.Error(resolveError!);
        }

        var macroId = resolvedId.Value;
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

        var scriptChanged = !string.IsNullOrWhiteSpace(script)
            && !string.Equals(script, existing.Content, StringComparison.Ordinal);

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

        MacroResource? updated;
        try
        {
            updated = await _mediator.Send(new PutCommand(existing), cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error($"Macro '{existing.Name}' could not be updated.");
        }

        var message = $"Macro '{updated.Name}' updated (id {updated.Id}).";
        if (scriptChanged && existing.Type != (int)MacroFunctionEnum.Custom)
        {
            message += " " + StandardMacroEditedHint;
        }

        return SkillResult.SuccessResult(
            new { updated.Id, updated.Name },
            message);
    }

    private async Task<(Guid? Id, string? Error)> ResolveMacroIdAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var rawId = GetParameter<string>(parameters, "macroId");
        if (!string.IsNullOrWhiteSpace(rawId))
        {
            return Guid.TryParse(rawId, out var parsed)
                ? (parsed, null)
                : (null, $"'{rawId}' is not a valid macro id.");
        }

        var name = GetParameter<string>(parameters, "macroName");
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, "Either macroId or macroName must be provided.");
        }

        var macros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();

        var (match, error) = MacroResolver.Resolve(macros, name);
        return match != null ? (match.Id, null) : (null, error);
    }
}
