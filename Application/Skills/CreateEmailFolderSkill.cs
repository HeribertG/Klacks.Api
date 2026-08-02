// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new custom email folder via CreateEmailFolderCommand, rejecting a duplicate name up
/// front with the list of existing folders, and confirming the write by re-reading the folder list
/// afterwards.
/// </summary>
/// <param name="name">Name of the new folder as it should appear in the folder tree (required).</param>
/// <param name="imapFolderName">Optional technical mailbox path on the mail server; defaults to name when omitted.</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class CreateEmailFolderSkill : BaseSkillImplementation
{
    private const string SkillName = "create_email_folder";

    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmailFolderSkill(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name").Trim();

        var imapFolderName = GetParameter<string>(parameters, "imapFolderName")?.Trim();
        if (string.IsNullOrWhiteSpace(imapFolderName))
        {
            imapFolderName = name;
        }

        var existingFolders = await _mediator.Send(new GetEmailFoldersQuery(), cancellationToken);
        var duplicate = existingFolders.FirstOrDefault(
            f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        if (duplicate != null)
        {
            var existingNames = string.Join(", ", existingFolders.Select(f => f.Name));
            return SkillResult.Error(
                $"A folder named '{name}' already exists. Existing folders: {existingNames}.");
        }

        EmailFolderResource created;
        try
        {
            created = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var result = await _mediator.Send(new CreateEmailFolderCommand(name, imapFolderName), cancellationToken);

                await ConfirmPersistedAsync(
                    SkillName,
                    async () =>
                    {
                        var refreshed = await _mediator.Send(new GetEmailFoldersQuery(), cancellationToken);
                        return refreshed.FirstOrDefault(f => f.Id == result.Id);
                    },
                    persisted => string.Equals(persisted.Name, name, StringComparison.OrdinalIgnoreCase),
                    $"the new folder '{name}'");

                return result;
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

        return SkillResult.SuccessResult(
            new { created.Id, created.Name, created.ImapFolderName, created.SortOrder },
            $"Email folder '{created.Name}' was created and confirmed in the database (verified).");
    }
}
