// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs the update the release check offers, through the same TriggerUpdateCommand the update card uses: it only
/// queues the operation, the out-of-process updater verifies, installs and restores on failure. It takes no parameters,
/// so neither a version nor an address can be named. It runs only as the confirmed replay of its server preview
/// (StartSoftwareUpdateConfirmationPreviewProvider): the version the preview showed travels in the binding and the command
/// refuses any other. Without that binding (a plan step, any context that bypasses the autonomy gate) it refuses.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class StartSoftwareUpdateSkill : BaseSkillImplementation
{
    public const string SkillName = "start_software_update";

    private const string NotStartedMessage = "The update was not started: {0}";

    private const string QueuedMessage =
        "The update to version {0} is queued and starts shortly. While it still waits it can be cancelled on the Software Updates card.";

    private readonly IMediator _mediator;

    public StartSoftwareUpdateSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public static ConfirmationBinding BindingFor(string targetVersion) =>
        ConfirmationBinding.Of(ConfirmationBindingKinds.SoftwareUpdate, [targetVersion]);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (context.ConfirmedActionBinding is not { Kind: ConfirmationBindingKinds.SoftwareUpdate, Items: [var targetVersion] })
        {
            return SkillResult.Error(
                string.Format(CultureInfo.InvariantCulture, ConfirmationBindingMessages.BindingRequired, SkillName));
        }

        var result = await _mediator.Send(
            new TriggerUpdateCommand(context.UserId.ToString(), targetVersion), cancellationToken);

        if (!result.Enqueued)
        {
            return SkillResult.Error(string.Format(CultureInfo.InvariantCulture, NotStartedMessage, result.Reason));
        }

        var queued = string.Format(CultureInfo.InvariantCulture, QueuedMessage, targetVersion);
        return SkillResult.SuccessResult(
            new { result.Enqueued, result.OperationId, TargetVersion = targetVersion },
            $"{queued} {SoftwareUpdateNotices.Restart} {SoftwareUpdateNotices.ResultOnCard}");
    }
}
