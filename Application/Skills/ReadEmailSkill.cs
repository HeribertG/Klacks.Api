// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads a single received email by ID via GetReceivedEmailQuery and returns subject, sender,
/// date and the plain-text body (HTML is stripped, long bodies are truncated). The read flag is
/// not changed and attachment contents are never returned.
/// </summary>
/// <param name="emailId">Required. UUID of the email to read (from list_emails).</param>

using System.Net;
using System.Text.RegularExpressions;
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
    private const string ScriptStyleBlockPattern = @"<(script|style)\b[^>]*>.*?</\1\s*>";
    private const string LineBreakTagPattern = @"<\s*(br\s*/?|/p|/div|/li|/tr|/h[1-6])\s*>";
    private const string AnyTagPattern = @"<[^>]+>";
    private const string ExcessBlankLinesPattern = @"(\s*\n){3,}";
    private const string ParagraphSeparator = "\n\n";
    private const string LineBreak = "\n";
    private static readonly TimeSpan StripTimeout = TimeSpan.FromSeconds(2);

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

        var body = !string.IsNullOrWhiteSpace(email.BodyText)
            ? email.BodyText.Trim()
            : StripHtml(email.BodyHtml);

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

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = Regex.Replace(html, ScriptStyleBlockPattern, string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline, StripTimeout);
        text = Regex.Replace(text, LineBreakTagPattern, LineBreak, RegexOptions.IgnoreCase, StripTimeout);
        text = Regex.Replace(text, AnyTagPattern, string.Empty, RegexOptions.Singleline, StripTimeout);
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, ExcessBlankLinesPattern, ParagraphSeparator, RegexOptions.None, StripTimeout);
        return text.Trim();
    }
}
