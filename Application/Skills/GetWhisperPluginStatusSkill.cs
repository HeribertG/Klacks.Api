// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the state of the self-hosted transcription plugin via GetWhisperPluginStatusQuery: whether
/// it is installed, which model size it runs, and whether an install or removal is under way.
/// </summary>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_whisper_plugin_status")]
public class GetWhisperPluginStatusSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetWhisperPluginStatusSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var status = await _mediator.Send(new GetWhisperPluginStatusQuery(), cancellationToken);

        var message = status.Installed
            ? $"Self-hosted transcription is installed, running the '{status.ModelAlias}' model."
            : "Self-hosted transcription is not installed.";

        if (status.ActiveOperation != null)
        {
            message += " An operation is currently running.";
        }

        return SkillResult.SuccessResult(
            new
            {
                status.Installed,
                status.ModelAlias,
                status.ModelId,
                status.ActiveOperation,
                status.LastOperation,
                status.OtherOperationActive
            },
            message);
    }
}
