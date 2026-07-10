// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Moves a received email into another folder — also the way to mark an email as spam
/// (move to the junk folder) or as not-spam (move back to the inbox). Accepts the special
/// aliases "inbox", "junk"/"spam" and "trash", which are resolved to the mailbox's real
/// IMAP folder names; any other value is used as a literal folder name. The move is
/// verified by re-reading the email afterwards.
/// </summary>
/// <param name="emailId">Required. UUID of the email (from list_emails).</param>
/// <param name="folder">Required. Target folder: "inbox", "junk"/"spam", "trash" or a literal folder name from list_email_folders.</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("move_email_to_folder")]
public class MoveEmailToFolderSkill : BaseSkillImplementation
{
    private const string SpamAlias = "spam";

    private readonly IMediator _mediator;
    private readonly IEmailFolderRepository _folderRepository;

    public MoveEmailToFolderSkill(IMediator mediator, IEmailFolderRepository folderRepository)
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
        var folderInput = GetRequiredString(parameters, "folder");

        var email = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (email == null)
        {
            return SkillResult.Error($"Email '{emailId}' not found.");
        }

        var (targetFolder, resolveError) = await ResolveFolderAsync(folderInput);
        if (resolveError != null)
        {
            return SkillResult.Error(resolveError);
        }

        if (string.Equals(email.Folder, targetFolder, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.SuccessResult(
                new { EmailId = emailId, email.Folder },
                $"Email '{email.Subject}' is already in folder '{targetFolder}' — nothing to move.");
        }

        var success = await _mediator.Send(new MoveEmailToFolderCommand(emailId, targetFolder!), cancellationToken);
        if (!success)
        {
            return SkillResult.Error($"Moving email '{emailId}' to folder '{targetFolder}' failed.");
        }

        var persisted = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (persisted == null || !string.Equals(persisted.Folder, targetFolder, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(
                $"Database verification failed: email '{emailId}' is not in folder '{targetFolder}' after the " +
                "write — treat the move as not persisted.");
        }

        return SkillResult.SuccessResult(
            new { EmailId = emailId, persisted.Subject, persisted.Folder, PreviousFolder = email.Folder },
            $"Email '{persisted.Subject}' was moved from '{email.Folder}' to '{persisted.Folder}' and confirmed " +
            "in the database (verified).");
    }

    private async Task<(string? Folder, string? Error)> ResolveFolderAsync(string folderInput)
    {
        var normalized = folderInput.Trim();

        string? specialUse = normalized.ToLowerInvariant() switch
        {
            "inbox" => FolderSpecialUse.Inbox,
            "junk" or SpamAlias => FolderSpecialUse.Junk,
            "trash" => FolderSpecialUse.Trash,
            _ => null
        };

        if (specialUse == null)
        {
            if (!await _folderRepository.ExistsByImapNameAsync(normalized))
            {
                return (null,
                    $"Folder '{normalized}' does not exist. Use list_email_folders to see the available folders, " +
                    "or use the aliases inbox / junk / trash.");
            }

            return (normalized, null);
        }

        var imapName = await _folderRepository.GetImapNameBySpecialUseAsync(specialUse);
        if (string.IsNullOrEmpty(imapName))
        {
            return (null,
                $"The mailbox has no {specialUse} folder configured. Use list_email_folders to see the " +
                "available folders.");
        }

        return (imapName, null);
    }
}
