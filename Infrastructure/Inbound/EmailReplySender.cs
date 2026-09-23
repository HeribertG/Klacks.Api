// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sends a clarification question as an email reply over the configured SMTP account. The recipient is
/// the client's STORED address that matched the sender (never an address taken from the message
/// itself), so a forged sender header at most makes the real person receive the question. The subject
/// gets a single "Re: " and is flattened to one line (line breaks and control characters of the decoded
/// original subject become spaces). In-Reply-To and References thread the reply onto the original mail;
/// only strictly valid message ids (printable ASCII without whitespace or angle brackets, containing '@')
/// are used, so synthetic "{folder}-{uid}" ids and header-injection attempts are dropped, and a
/// References value containing line breaks or control characters is discarded as a whole. Every reply
/// carries Auto-Submitted: auto-replied (RFC 3834) so the employee's auto-responder does not answer it.
/// Header values with line breaks, SMTP errors, an unavailable mail service and exceptions map to a
/// failed result, never an exception.
/// </summary>
/// <param name="assignmentService">Resolves the stored address of the client</param>
/// <param name="emailService">Sends the mail over the configured SMTP account</param>
/// <param name="logger">Logs failed sends</param>

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Infrastructure.Inbound;

public sealed partial class EmailReplySender : IInboundReplySender
{
    private const string MailServiceUnavailableError = "The mail service is not available";
    private const string LineBreakInHeaderError = "A reply header contains a line break or control character";
    private const char MessageIdSeparator = ' ';
    private const char MessageIdOpen = '<';
    private const char MessageIdClose = '>';
    private const char SubjectSeparator = ' ';
    private const string ValidMessageIdPattern = @"\A[!-;=?-~]+@[!-;=?-~]+\z";

    private readonly IEmailClientAssignmentService _assignmentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailReplySender> _logger;

    public EmailReplySender(
        IEmailClientAssignmentService assignmentService,
        IEmailService emailService,
        ILogger<EmailReplySender> logger)
    {
        _assignmentService = assignmentService;
        _emailService = emailService;
        _logger = logger;
    }

    public InboundSourceKind SourceKind => InboundSourceKind.Email;

    public async Task<InboundReplyTarget?> ResolveTargetAsync(ClarificationRequest request, CancellationToken cancellationToken = default)
    {
        var storedAddress = await _assignmentService.GetStoredAddressAsync(request.ClientId, request.SenderAddress, cancellationToken);
        if (string.IsNullOrWhiteSpace(storedAddress))
        {
            return null;
        }

        var thread = request.EmailThread;
        var messageId = thread == null ? null : Unbracket(thread.MessageId);
        var inReplyTo = messageId != null && IsValidMessageId(messageId) ? Bracket(messageId) : null;

        return new InboundReplyTarget(
            Recipient: storedAddress,
            Subject: BuildSubject(request.Source.Subject),
            InReplyTo: inReplyTo,
            References: BuildReferences(thread));
    }

    public async Task<InboundReplyResult> SendAsync(
        ClarificationRequest request, InboundReplyTarget target, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var headers = BuildHeaders(target);
            if (headers.Values.Any(ContainsForbiddenHeaderCharacter) ||
                ContainsForbiddenHeaderCharacter(target.Subject) ||
                ContainsForbiddenHeaderCharacter(target.Recipient))
            {
                _logger.LogWarning("Clarification email to client {ClientId} refused: header value with a line break or control character", request.ClientId);
                return InboundReplyResult.Failed(LineBreakInHeaderError);
            }

            if (!await _emailService.CanSendEmailAsync())
            {
                return InboundReplyResult.Failed(MailServiceUnavailableError);
            }

            var result = _emailService.SendReplyMail(target.Recipient, target.Subject ?? string.Empty, text, headers);
            if (result == EmailConstants.SendSucceededResult)
            {
                return InboundReplyResult.Sent;
            }

            _logger.LogWarning("Clarification email to client {ClientId} failed: {Error}", request.ClientId, result);
            return InboundReplyResult.Failed(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clarification email to client {ClientId} failed", request.ClientId);
            return InboundReplyResult.Failed(ex.Message);
        }
    }

    internal static string BuildSubject(string? originalSubject)
    {
        var subject = FlattenToSingleLine(originalSubject);
        return subject.StartsWith(InboundClarificationConstants.ReplySubjectMarker, StringComparison.OrdinalIgnoreCase)
            ? subject
            : InboundClarificationConstants.ReplySubjectPrefix + subject;
    }

    internal static string? BuildReferences(ClarificationEmailThread? thread)
    {
        if (thread == null)
        {
            return null;
        }

        var references = thread.ThreadReferences ?? string.Empty;
        var referencedIds = references.Any(IsForbiddenInHeader)
            ? Array.Empty<string>()
            : references.Split(MessageIdSeparator, StringSplitOptions.RemoveEmptyEntries);

        var ids = referencedIds
            .Append(thread.MessageId)
            .Select(Unbracket)
            .Where(IsValidMessageId)
            .Distinct(StringComparer.Ordinal)
            .Select(Bracket)
            .ToList();

        return ids.Count > 0 ? string.Join(MessageIdSeparator, ids) : null;
    }

    private static IReadOnlyDictionary<string, string> BuildHeaders(InboundReplyTarget target)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [InboundClarificationConstants.AutoSubmittedHeader] = InboundClarificationConstants.AutoSubmittedReplyValue
        };

        if (!string.IsNullOrWhiteSpace(target.InReplyTo))
        {
            headers[InboundClarificationConstants.InReplyToHeader] = target.InReplyTo;
        }

        if (!string.IsNullOrWhiteSpace(target.References))
        {
            headers[InboundClarificationConstants.ReferencesHeader] = target.References;
        }

        return headers;
    }

    private static string FlattenToSingleLine(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var pendingSeparator = false;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character) || IsForbiddenInHeader(character))
            {
                pendingSeparator = builder.Length > 0;
                continue;
            }

            if (pendingSeparator)
            {
                builder.Append(SubjectSeparator);
                pendingSeparator = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool IsForbiddenInHeader(char character) =>
        char.IsControl(character) ||
        char.GetUnicodeCategory(character) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;

    private static bool ContainsForbiddenHeaderCharacter(string? value) => value != null && value.Any(IsForbiddenInHeader);

    private static bool IsValidMessageId(string? messageId) =>
        !string.IsNullOrEmpty(messageId) && ValidMessageId().IsMatch(messageId);

    private static string? Unbracket(string? messageId)
    {
        if (messageId == null)
        {
            return null;
        }

        var start = messageId.StartsWith(MessageIdOpen) ? 1 : 0;
        var end = messageId.EndsWith(MessageIdClose) && messageId.Length > start ? messageId.Length - 1 : messageId.Length;
        return messageId[start..end];
    }

    private static string Bracket(string? messageId) => MessageIdOpen + messageId + MessageIdClose;

    [GeneratedRegex(ValidMessageIdPattern, RegexOptions.CultureInvariant)]
    private static partial Regex ValidMessageId();
}
