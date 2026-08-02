// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a custom email folder resolved by name via DeleteEmailFolderCommand. Refuses ambiguous
/// names and built-in mailboxes, reports what happens to contained emails (their local Klacks copy
/// is relabeled to trash, but the real folder and its messages are removed from the mail server),
/// and confirms the removal by re-reading the folder list afterwards.
/// </summary>
/// <param name="folderName">Name of the folder to delete, as shown in the folder tree (required).</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class DeleteEmailFolderSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_email_folder";

    private readonly IMediator _mediator;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmailFolderSkill(
        IMediator mediator,
        IEmailFolderRepository folderRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _folderRepository = folderRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var folderName = GetRequiredString(parameters, "folderName").Trim();

        var allFolders = await _mediator.Send(new GetEmailFoldersQuery(), cancellationToken);
        var matches = allFolders
            .Where(f => string.Equals(f.Name, folderName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            var existingNames = string.Join(", ", allFolders.Select(f => f.Name));
            return SkillResult.Error(
                $"No folder named '{folderName}' found. Existing folders: {existingNames}.");
        }

        if (matches.Count > 1)
        {
            var candidates = string.Join(", ", matches.Select(f => $"{f.Name} (id {f.Id})"));
            return SkillResult.Error(
                $"Multiple folders are named '{folderName}': {candidates}. " +
                "Nothing was deleted — specify the folder more precisely.");
        }

        var folder = matches[0];

        if (folder.IsSystem)
        {
            return SkillResult.Error(
                $"'{folder.Name}' is a built-in mailbox (e.g. inbox, sent, junk or trash) and cannot be deleted.");
        }

        var trashImapName = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Trash);
        var trashConfigured = !string.IsNullOrEmpty(trashImapName);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var removed = await _mediator.Send(new DeleteEmailFolderCommand(folder.Id), cancellationToken);
                if (!removed)
                {
                    throw new SkillVerificationException(SkillName, $"Folder '{folder.Name}' was not found anymore.");
                }

                await ConfirmDeletedAsync(
                    SkillName,
                    async () =>
                    {
                        var refreshed = await _mediator.Send(new GetEmailFoldersQuery(), cancellationToken);
                        return refreshed.FirstOrDefault(f => f.Id == folder.Id);
                    },
                    $"folder '{folder.Name}'");

                return removed;
            });
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }
        catch (SkillVerificationException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        var mailNote = folder.TotalCount > 0
            ? trashConfigured
                ? $" It contained {folder.TotalCount} email(s): their locally stored copies were relabeled to the trash folder in Klacks, but the folder and its messages were permanently removed from the mail server itself."
                : $" Warning: it contained {folder.TotalCount} email(s); no trash folder is configured, so their local folder reference was left pointing at the deleted folder, and the folder together with its messages was permanently removed from the mail server."
            : string.Empty;

        return SkillResult.SuccessResult(
            new { FolderId = folder.Id, folder.Name, folder.TotalCount },
            $"Email folder '{folder.Name}' was deleted and confirmed removed from the database (verified).{mailNote}");
    }
}
