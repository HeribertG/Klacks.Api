// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks a received email as read or unread (the chat equivalent of the inbox context menu).
/// The change is verified by re-reading the email afterwards; a mismatch is reported as an
/// error instead of a fabricated success.
/// </summary>
/// <param name="emailId">Required. UUID of the email (from list_emails).</param>
/// <param name="isRead">Optional. true marks as read (default), false marks as unread.</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("mark_email_read")]
public class MarkEmailReadSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public MarkEmailReadSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var emailId = GetRequiredGuid(parameters, "emailId");
        var isRead = GetParameter<bool?>(parameters, "isRead") ?? true;

        var email = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (email == null)
        {
            return SkillResult.Error($"Email '{emailId}' not found.");
        }

        if (email.IsRead == isRead)
        {
            return SkillResult.SuccessResult(
                new { EmailId = emailId, email.IsRead },
                $"Email '{email.Subject}' is already marked as {(isRead ? "read" : "unread")} — nothing to change.");
        }

        var success = await _mediator.Send(
            new MarkEmailAsReadCommand(emailId, isRead, context.UserId.ToString()), cancellationToken);
        if (!success)
        {
            return SkillResult.Error($"Marking email '{emailId}' as {(isRead ? "read" : "unread")} failed.");
        }

        var persisted = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (persisted == null || persisted.IsRead != isRead)
        {
            return SkillResult.Error(
                $"Database verification failed: email '{emailId}' does not show the new read state after the " +
                "write — treat the change as not persisted.");
        }

        return SkillResult.SuccessResult(
            new { EmailId = emailId, persisted.Subject, persisted.IsRead },
            $"Email '{persisted.Subject}' was marked as {(isRead ? "read" : "unread")} and confirmed in the " +
            "database (verified).");
    }
}
