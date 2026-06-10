// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists all email inbox folders with unread and total counts via GetEmailFoldersQuery.
/// Use the returned imapFolderName as the folder parameter of list_emails.
/// </summary>

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_email_folders")]
public class ListEmailFoldersSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListEmailFoldersSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var folders = await _mediator.Send(new GetEmailFoldersQuery(), cancellationToken);

        var projected = folders
            .Select(f => new { f.Id, f.Name, f.ImapFolderName, f.IsSystem, f.UnreadCount, f.TotalCount })
            .ToList();

        var totalUnread = projected.Sum(f => f.UnreadCount);

        return SkillResult.SuccessResult(
            new { Count = projected.Count, TotalUnread = totalUnread, Folders = projected },
            $"Found {projected.Count} email folders with {totalUnread} unread emails in total. Use list_emails with an imapFolderName to list the emails of one folder.");
    }
}
