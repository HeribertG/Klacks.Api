// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Email;

public class ReceivedEmail : BaseEntity
{
    public string MessageId { get; set; } = string.Empty;

    public long ImapUid { get; set; }

    public string Folder { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string ToAddress { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string? BodyHtml { get; set; }

    public string? BodyText { get; set; }

    public DateTime ReceivedDate { get; set; }

    public bool IsRead { get; set; }

    public bool HasAttachments { get; set; }
}
