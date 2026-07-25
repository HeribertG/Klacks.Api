// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Email;

public class ReceivedEmail : BaseEntity
{
    public string MessageId { get; set; } = string.Empty;

    public long ImapUid { get; set; }

    public string Folder { get; set; } = string.Empty;

    public string SourceImapFolder { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string ToAddress { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string? BodyHtml { get; set; }

    public string? BodyText { get; set; }

    public DateTime ReceivedDate { get; set; }

    public bool IsRead { get; set; }

    public bool HasAttachments { get; set; }

    /// <summary>
    /// Set once spam-classification, client assignment and intent analysis have all been attempted for
    /// this email (regardless of outcome — no client match still counts as processed). Null means the
    /// email is still in the processing backlog, e.g. because the polling cycle that fetched it was
    /// interrupted before finishing; EmailPollingBackgroundService retries every unprocessed row on
    /// each cycle rather than relying on IMAP UID novelty, which only fires once per message.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
