// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the group/client hierarchy of assigned emails with email and unread counters via
/// GetEmailGroupTreeQuery, so the caller can see where incoming mail piles up.
/// </summary>

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_email_group_tree")]
public class GetEmailGroupTreeSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetEmailGroupTreeSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var tree = await _mediator.Send(new GetEmailGroupTreeQuery(), cancellationToken);

        var projected = tree.Select(ProjectNode).ToList();

        var totalEmails = tree.Sum(n => n.EmailCount);
        var totalUnread = tree.Sum(AggregateUnread);

        return SkillResult.SuccessResult(
            new { TotalEmails = totalEmails, TotalUnread = totalUnread, Tree = projected },
            $"{totalEmails} email(s) are assigned to clients and groups, {totalUnread} of them unread.");
    }

    private static int AggregateUnread(EmailGroupTreeNode node)
    {
        return node.Type == EmailGroupNodeType.Client
            ? node.UnreadCount
            : node.Children.Sum(AggregateUnread);
    }

    private static object ProjectNode(EmailGroupTreeNode node)
    {
        return new
        {
            node.Id,
            node.Name,
            Type = node.Type.ToString(),
            node.EmailCount,
            UnreadCount = AggregateUnread(node),
            Children = node.Children.Select(ProjectNode).ToList()
        };
    }
}
