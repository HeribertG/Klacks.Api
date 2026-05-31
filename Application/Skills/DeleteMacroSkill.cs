// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a calculation macro (script) from the settings. Thin wrapper around
/// <see cref="Klacks.Api.Application.Commands.Settings.Macros.DeleteCommand"/>; the macro can be
/// identified by macroId (preferred) or by an exact, unambiguous name resolved via
/// <see cref="Klacks.Api.Application.Queries.Settings.Macros.ListQuery"/>.
/// </summary>
/// <param name="macroId">Optional. The id of the macro to delete. Preferred over macroName.</param>
/// <param name="macroName">Optional. The exact macro name used to resolve the macro when macroId is omitted.</param>

using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_macro")]
public class DeleteMacroSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteMacroSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (macroId, resolveError) = await ResolveMacroIdAsync(parameters, cancellationToken);
        if (macroId == null)
        {
            return SkillResult.Error(resolveError!);
        }

        var deleted = await _mediator.Send(new DeleteCommand(macroId.Value), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Macro with id '{macroId}' could not be deleted.");
        }

        return SkillResult.SuccessResult(
            new { deleted.Id, deleted.Name },
            $"Macro '{deleted.Name}' deleted.");
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

        var exact = macros
            .Where(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var candidates = exact.Count > 0
            ? exact
            : macros.Where(m => m.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (candidates.Count == 0)
        {
            return (null, $"No macro found matching '{name}'.");
        }

        if (candidates.Count > 1)
        {
            var names = string.Join(", ", candidates.Select(m => $"{m.Name} ({m.Id})"));
            return (null, $"'{name}' is ambiguous. Matching macros: {names}. Provide macroId instead.");
        }

        return (candidates[0].Id, null);
    }
}
