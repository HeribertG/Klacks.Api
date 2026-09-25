// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a calculation macro (script) from the settings. Thin wrapper around
/// <see cref="Klacks.Api.Application.Commands.Settings.Macros.DeleteCommand"/>; the macro can be
/// identified by macroId (preferred) or by name, resolved fuzzily and unambiguously via
/// <see cref="MacroResolver"/> over <see cref="Klacks.Api.Application.Queries.Settings.Macros.ListQuery"/>.
/// Only macros owned by the assistant (origin Assistant or AssistantExtension) may be deleted;
/// <see cref="MacroAssistantGuard"/> refuses all others. A delete refused by the reference check (shifts or
/// absence types still use the macro) is relayed with its message. An unknown id is reported as not found: the
/// GetQuery handler answers with null for a missing macro, it does not throw.
/// </summary>
/// <param name="macroId">Optional. The id of the macro to delete. Preferred over macroName.</param>
/// <param name="macroName">Optional. The macro name used to resolve the macro when macroId is omitted.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_macro")]
public class DeleteMacroSkill : BaseSkillImplementation
{
    private const string MacroIdParameter = "macroId";
    private const string MacroNameParameter = "macroName";
    private const string InvalidIdMessage = "'{0}' is not a valid macro id.";
    private const string SourceMissingMessage = "Either macroId or macroName must be provided.";
    private const string NotFoundMessage = "No macro found with id '{0}'.";
    private const string NotDeletedMessage = "Macro with id '{0}' could not be deleted.";
    private const string DeletedMessage = "Macro '{0}' deleted.";

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

        var existing = await _mediator.Send(new GetQuery(macroId.Value), cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error(Format(NotFoundMessage, macroId.Value));
        }

        MacroResource? deleted;
        try
        {
            MacroAssistantGuard.EnsureMayDelete(existing);
            deleted = await _mediator.Send(new DeleteCommand(macroId.Value), cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (deleted == null)
        {
            return SkillResult.Error(Format(NotDeletedMessage, macroId.Value));
        }

        return SkillResult.SuccessResult(
            new { deleted.Id, deleted.Name },
            Format(DeletedMessage, deleted.Name));
    }

    private async Task<(Guid? Id, string? Error)> ResolveMacroIdAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var rawId = GetParameter<string>(parameters, MacroIdParameter);
        if (!string.IsNullOrWhiteSpace(rawId))
        {
            return Guid.TryParse(rawId, out var parsed)
                ? (parsed, null)
                : (null, Format(InvalidIdMessage, rawId));
        }

        var name = GetParameter<string>(parameters, MacroNameParameter);
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, SourceMissingMessage);
        }

        var macros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();

        var (match, error) = MacroResolver.Resolve(macros, name);
        return match != null ? (match.Id, null) : (null, error);
    }

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
