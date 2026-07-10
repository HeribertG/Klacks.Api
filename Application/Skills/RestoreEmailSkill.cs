// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Restores a received email from the trash back to the inbox. The restore is verified by
/// re-reading the email afterwards; a mismatch is reported as an error.
/// </summary>
/// <param name="emailId">Required. UUID of the email in the trash (from list_emails with folder=trash).</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("restore_email")]
public class RestoreEmailSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IEmailFolderRepository _folderRepository;

    public RestoreEmailSkill(IMediator mediator, IEmailFolderRepository folderRepository)
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
        if (string.IsNullOrEmpty(trashFolder)
            || !string.Equals(email.Folder, trashFolder, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(
                $"Email '{email.Subject}' is not in the trash (current folder: '{email.Folder}') — " +
                "only trashed emails can be restored.");
        }

        var success = await _mediator.Send(new RestoreEmailCommand(emailId), cancellationToken);
        if (!success)
        {
            return SkillResult.Error($"Restoring email '{emailId}' failed.");
        }

        var persisted = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (persisted == null
            || string.Equals(persisted.Folder, trashFolder, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(
                $"Database verification failed: email '{emailId}' is still in the trash after the restore — " +
                "treat the restore as not persisted.");
        }

        return SkillResult.SuccessResult(
            new { EmailId = emailId, persisted.Subject, persisted.Folder },
            $"Email '{persisted.Subject}' was restored to folder '{persisted.Folder}' and confirmed in the " +
            "database (verified).");
    }
}
