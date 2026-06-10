// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists received emails (newest first) via GetReceivedEmailsQuery as compact
/// sender/subject/date projections — body content is never included, use read_email for that.
/// </summary>
/// <param name="folder">Optional. IMAP folder name to filter by (see list_email_folders); omit for all folders.</param>
/// <param name="maxResults">Optional. Maximum number of emails to return (1-50); defaults to 20.</param>
/// <param name="unreadOnly">Optional. When true, only unread emails are returned.</param>

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_emails")]
public class ListEmailsSkill : BaseSkillImplementation
{
    private const string FolderParameter = "folder";
    private const string MaxResultsParameter = "maxResults";
    private const string UnreadOnlyParameter = "unreadOnly";
    private const string UnreadFilterValue = "unread";
    private const int DefaultMaxResults = 20;
    private const int MinResults = 1;
    private const int MaxResults = 50;
    private const int SkipNone = 0;

    private readonly IMediator _mediator;

    public ListEmailsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var folder = GetParameter<string>(parameters, FolderParameter);
        var take = Math.Clamp(GetParameter<int?>(parameters, MaxResultsParameter) ?? DefaultMaxResults, MinResults, MaxResults);
        var unreadOnly = GetParameter<bool?>(parameters, UnreadOnlyParameter) ?? false;

        var response = await _mediator.Send(
            new GetReceivedEmailsQuery(SkipNone, take, folder, unreadOnly ? UnreadFilterValue : null),
            cancellationToken);

        var projected = response.Items
            .Select(e => new
            {
                e.Id,
                From = FormatSender(e.FromName, e.FromAddress),
                e.Subject,
                Received = e.ReceivedDate,
                e.IsRead,
                e.HasAttachments
            })
            .ToList();

        var scope = string.IsNullOrWhiteSpace(folder) ? "all folders" : $"folder '{folder}'";

        return SkillResult.SuccessResult(
            new { Count = projected.Count, response.TotalCount, response.UnreadCount, Emails = projected },
            $"Showing {projected.Count} of {response.TotalCount} emails in {scope} ({response.UnreadCount} unread). Use read_email with an email ID to read the content.");
    }

    private static string FormatSender(string? fromName, string fromAddress)
    {
        return string.IsNullOrWhiteSpace(fromName) ? fromAddress : $"{fromName} <{fromAddress}>";
    }
}
