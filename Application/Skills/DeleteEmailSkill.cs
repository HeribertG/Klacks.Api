// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Moves a received email into the trash folder (reversible — use restore_email to bring it
/// back). There is deliberately no skill for permanent deletion; that stays a manual UI
/// action. The delete is verified by re-reading the email afterwards.
/// </summary>
/// <param name="emailId">Required. UUID of the email (from list_emails).</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_email")]
public class DeleteEmailSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IEmailFolderRepository _folderRepository;

    public DeleteEmailSkill(IMediator mediator, IEmailFolderRepository folderRepository)
    {
        _mediator = mediator;
        _folderRepository = folderRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var emailId = GetRequiredGuid(parameters, "emailId");

        var email = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (email == null)
        {
            return SkillResult.Error($"Email '{emailId}' not found.");
        }

        var trashFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Trash);
        if (!string.IsNullOrEmpty(trashFolder)
            && string.Equals(email.Folder, trashFolder, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.SuccessResult(
                new { EmailId = emailId, email.Folder },
                $"Email '{email.Subject}' is already in the trash — nothing to delete.");
        }

        var success = await _mediator.Send(new DeleteReceivedEmailCommand(emailId), cancellationToken);
        if (!success)
        {
            return SkillResult.Error($"Deleting email '{emailId}' failed.");
        }

        var persisted = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (persisted == null
            || string.IsNullOrEmpty(trashFolder)
            || !string.Equals(persisted.Folder, trashFolder, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(
                $"Database verification failed: email '{emailId}' is not in the trash folder after the delete — " +
                "treat the delete as not persisted.");
        }

        return SkillResult.SuccessResult(
            new { EmailId = emailId, persisted.Subject, persisted.Folder },
            $"Email '{persisted.Subject}' was moved to the trash and confirmed in the database (verified). " +
            "It can be brought back with restore_email.");
    }
}
