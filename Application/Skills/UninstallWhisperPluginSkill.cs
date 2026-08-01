// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes the self-hosted transcription plugin via UninstallWhisperPluginCommand. The work runs in
/// the background, so the skill reports whether it was queued rather than claiming it is finished.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("uninstall_whisper_plugin")]
public class UninstallWhisperPluginSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UninstallWhisperPluginSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new UninstallWhisperPluginCommand(context.UserId.ToString()), cancellationToken);

        if (!result.Enqueued)
        {
            return SkillResult.Error($"Removal was not started: {result.Reason}");
        }

        return SkillResult.SuccessResult(
            new { result.Enqueued, result.OperationId },
            "Removal of the self-hosted transcription model was queued and runs in the background.");
    }
}
