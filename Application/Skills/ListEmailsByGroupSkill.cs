// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the received emails on file for a group and its subgroups, resolved by name via
/// GetEmailsByGroupQuery, as compact sender/subject/date projections.
/// </summary>
/// <param name="groupName">Display name of the group (required); resolved with fuzzy matching.</param>
/// <param name="maxResults">Optional. Maximum number of emails to return (1-50); defaults to 20.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_emails_by_group")]
public class ListEmailsByGroupSkill : BaseSkillImplementation
{
    private const string GroupNameParameter = "groupName";
    private const string MaxResultsParameter = "maxResults";
    private const int DefaultMaxResults = 20;
    private const int MinResults = 1;
    private const int MaxResults = 50;
    private const int SkipNone = 0;

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;

    public ListEmailsByGroupSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IMediator mediator)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, GroupNameParameter);

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List());
        var (group, resolveError) = GroupResolver.Resolve(groups, groupName);
        if (resolveError != null)
        {
            return SkillResult.Error(resolveError);
        }

        var take = Math.Clamp(
            GetParameter<int?>(parameters, MaxResultsParameter) ?? DefaultMaxResults, MinResults, MaxResults);

        var response = await _mediator.Send(
            new GetEmailsByGroupQuery(group!.Id, SkipNone, take), cancellationToken);

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

        return SkillResult.SuccessResult(
            new
            {
                GroupId = group.Id,
                GroupName = group.Name,
                Count = projected.Count,
                response.TotalCount,
                response.UnreadCount,
                Emails = projected
            },
            $"Group '{group.Name}' has {response.TotalCount} email(s) on file across its members " +
            $"({response.UnreadCount} unread). Showing {projected.Count}.");
    }

    private static string FormatSender(string? fromName, string fromAddress)
    {
        return string.IsNullOrWhiteSpace(fromName) ? fromAddress : $"{fromName} <{fromAddress}>";
    }
}
