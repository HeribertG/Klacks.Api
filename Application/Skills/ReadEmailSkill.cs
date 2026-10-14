// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a single received email by ID via GetReceivedEmailQuery and returns subject, sender,
/// date and the plain-text body (HTML is stripped, long bodies are truncated). The read flag is
/// not changed and attachment contents are never returned.
/// </summary>
/// <param name="emailId">Required. UUID of the email to read (from list_emails).</param>

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("read_email")]
public class ReadEmailSkill : BaseSkillImplementation
{
    private const string EmailIdParameter = "emailId";
    private const int MaxBodyLength = 4000;
    private const string TruncationMarker = " … [body truncated]";

    private readonly IMediator _mediator;

    public ReadEmailSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var emailId = GetRequiredGuid(parameters, EmailIdParameter);

        var email = await _mediator.Send(new GetReceivedEmailQuery(emailId), cancellationToken);
        if (email == null)
        {
            return SkillResult.Error($"Email '{emailId}' not found.");
        }

        var body = EmailBodyPlainText.From(email.BodyText, email.BodyHtml);

        var truncated = body.Length > MaxBodyLength;
        if (truncated)
        {
            body = body[..MaxBodyLength] + TruncationMarker;
        }

        var from = string.IsNullOrWhiteSpace(email.FromName)
            ? email.FromAddress
            : $"{email.FromName} <{email.FromAddress}>";

        var attachmentNote = email.HasAttachments
            ? " The email has attachments; attachment contents are not available here."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                email.Id,
                email.Subject,
                From = from,
                To = email.ToAddress,
                Received = email.ReceivedDate,
                email.Folder,
                email.IsRead,
                email.HasAttachments,
                Body = body,
                BodyTruncated = truncated
            },
            $"Email '{email.Subject}' from {from}, received {email.ReceivedDate:yyyy-MM-dd HH:mm}.{attachmentNote}");
    }
}
