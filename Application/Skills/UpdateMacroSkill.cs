// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing calculation macro (script) in the settings. The macro is identified by
/// macroId (preferred) or by name, resolved fuzzily and unambiguously via <see cref="MacroResolver"/>
/// over <see cref="Klacks.Api.Application.Queries.Settings.Macros.ListQuery"/>. Loads the macro via
/// <see cref="Klacks.Api.Application.Queries.Settings.Macros.GetQuery"/>, merges only the provided
/// fields onto the current values and persists the result via
/// <see cref="Klacks.Api.Application.Commands.Settings.Macros.PutCommand"/> marked as an assistant edit, so the
/// macro keeps its origin. When the script of a macro carrying a standard function is changed, the success message
/// warns that the macro now counts as customer-owned and future automatic region-setup updates will fail with a
/// conflict. <see cref="MacroAssistantGuard"/> decides what may be changed: macros created by the assistant freely,
/// extended copies only in name and description, all other macros not at all. A new script must OUTPUT only on the
/// channels the backend processes (<see cref="MacroOutputChannelPolicy"/>), and a new name must be free
/// (<see cref="MacroNameCollision"/>; the macro may keep its own name).
/// </summary>
/// <param name="macroId">Optional. The id of the macro to update. Preferred over macroName.</param>
/// <param name="macroName">Optional. The macro name used to resolve the macro when macroId is omitted.</param>
/// <param name="name">Optional. New macro name.</param>
/// <param name="script">Optional. New macro script body (stored as content).</param>
/// <param name="description">Optional. New description applied to all core languages.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_macro")]
public class UpdateMacroSkill : BaseSkillImplementation
{
    private const string MacroIdParameter = "macroId";
    private const string MacroNameParameter = "macroName";
    private const string NameParameter = "name";
    private const string ScriptParameter = "script";
    private const string ContentParameter = "content";
    private const string DescriptionParameter = "description";

    private const string NothingToUpdateMessage =
        "Nothing to update. Provide at least one of name, script, or description.";
    private const string InvalidIdMessage = "'{0}' is not a valid macro id.";
    private const string SourceMissingMessage = "Either macroId or macroName must be provided.";
    private const string NotFoundMessage = "No macro found with id '{0}'.";
    private const string NotUpdatedMessage = "Macro '{0}' could not be updated.";
    private const string UpdatedMessage = "Macro '{0}' updated (id {1}).";
    private const string StandardMacroEditedHint =
        "Note: this macro carries a standard function and is normally kept in sync automatically by the "
        + "region setup. After this manual script change it counts as customer-owned, and future automatic "
        + "updates of this macro will fail with a conflict. Please point this out to the user.";

    private readonly IMediator _mediator;
    private readonly IMacroOutputChannelInspector _channelInspector;

    public UpdateMacroSkill(IMediator mediator, IMacroOutputChannelInspector channelInspector)
    {
        _mediator = mediator;
        _channelInspector = channelInspector;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, NameParameter);
        var script = GetParameter<string>(parameters, ScriptParameter)
                     ?? GetParameter<string>(parameters, ContentParameter);
        var description = GetParameter<string>(parameters, DescriptionParameter);

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(script)
            && string.IsNullOrWhiteSpace(description))
        {
            return SkillResult.Error(NothingToUpdateMessage);
        }

        var (resolvedId, resolveError) = await ResolveMacroIdAsync(parameters, cancellationToken);
        if (resolvedId == null)
        {
            return SkillResult.Error(resolveError!);
        }

        var existing = await _mediator.Send(new GetQuery(resolvedId.Value), cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error(Format(NotFoundMessage, resolvedId.Value));
        }

        var scriptChanged = !string.IsNullOrWhiteSpace(script)
            && !string.Equals(script, existing.Content, StringComparison.Ordinal);

        var refusal = await FindRefusalAsync(existing, name, script, scriptChanged, cancellationToken);
        if (refusal != null)
        {
            return SkillResult.Error(refusal);
        }

        ApplyChanges(existing, name, script, description);

        MacroResource? updated;
        try
        {
            updated = await _mediator.Send(new PutCommand(existing, ByAssistant: true), cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error(Format(NotUpdatedMessage, existing.Name));
        }

        var message = Format(UpdatedMessage, updated.Name, updated.Id);
        if (scriptChanged && existing.Type != (int)MacroFunctionEnum.Custom)
        {
            message += " " + StandardMacroEditedHint;
        }

        return SkillResult.SuccessResult(
            new { updated.Id, updated.Name },
            message);
    }

    private async Task<string?> FindRefusalAsync(
        MacroResource existing,
        string? name,
        string? script,
        bool scriptChanged,
        CancellationToken cancellationToken)
    {
        try
        {
            MacroAssistantGuard.EnsureMayUpdate(existing, scriptChanged);
        }
        catch (InvalidRequestException ex)
        {
            return ex.Message;
        }

        if (scriptChanged)
        {
            var channelViolation = MacroOutputChannelPolicy.FindViolation(_channelInspector.Inspect(script!));
            if (channelViolation != null)
            {
                return channelViolation;
            }
        }

        if (string.IsNullOrWhiteSpace(name)
            || string.Equals(name.Trim(), existing.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var macros = await _mediator.Send(new ListQuery(), cancellationToken);
        return MacroNameCollision.FindRefusal(macros ?? [], name, existing.Id);
    }

    private static void ApplyChanges(MacroResource existing, string? name, string? script, string? description)
    {
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

        var macroName = GetParameter<string>(parameters, MacroNameParameter);
        if (string.IsNullOrWhiteSpace(macroName))
        {
            return (null, SourceMissingMessage);
        }

        var macros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();

        var (match, error) = MacroResolver.Resolve(macros, macroName);
        return match != null ? (match.Id, null) : (null, error);
    }

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
