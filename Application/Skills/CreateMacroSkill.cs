// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a calculation macro (script) in the settings on behalf of the assistant. Thin wrapper around
/// <see cref="Klacks.Api.Application.Commands.Settings.Macros.PostCommand"/> with origin Assistant, so the
/// assistant may later change or delete it. Before anything is stored, the OUTPUT channels of the script are
/// checked: only the channels the backend processes are accepted (<see cref="MacroOutputChannelPolicy"/>).
/// </summary>
/// <param name="name">Required. The macro name.</param>
/// <param name="script">Required. The macro script body (stored as content).</param>
/// <param name="description">Optional. A short description applied to all core languages.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_macro")]
public class CreateMacroSkill : BaseSkillImplementation
{
    private const string NameParameter = "name";
    private const string ScriptParameter = "script";
    private const string DescriptionParameter = "description";
    private const string NameRequiredMessage = "name is required.";
    private const string ScriptRequiredMessage = "script is required.";
    private const string NotCreatedMessage = "Macro '{0}' could not be created.";
    private const string CreatedMessage = "Macro '{0}' created (id {1}).";

    private readonly IMediator _mediator;
    private readonly IMacroOutputChannelInspector _channelInspector;

    public CreateMacroSkill(IMediator mediator, IMacroOutputChannelInspector channelInspector)
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
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error(NameRequiredMessage);
        }

        var script = GetParameter<string>(parameters, ScriptParameter);
        if (string.IsNullOrWhiteSpace(script))
        {
            return SkillResult.Error(ScriptRequiredMessage);
        }

        var channelViolation = MacroOutputChannelPolicy.FindViolation(_channelInspector.Inspect(script));
        if (channelViolation != null)
        {
            return SkillResult.Error(channelViolation);
        }

        var resource = new MacroResource
        {
            Name = name.Trim(),
            Content = script,
            Type = (int)MacroFunctionEnum.Custom,
            Description = MacroDescriptionFactory.ForAllCoreLanguages(GetParameter<string>(parameters, DescriptionParameter))
        };

        MacroResource? created;
        try
        {
            created = await _mediator.Send(new PostCommand(resource, MacroOrigin.Assistant), cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (created == null)
        {
            return SkillResult.Error(Format(NotCreatedMessage, name.Trim()));
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.Name },
            Format(CreatedMessage, created.Name, created.Id));
    }

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
