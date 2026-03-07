// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class ReceivedEmailResource
{
    public Guid Id { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public long ImapUid { get; set; }

    public string Folder { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string ToAddress { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string? BodyHtml { get; set; }

    public string? BodyText { get; set; }

    private DateTime _receivedDate;
    public DateTime ReceivedDate
    {
        get => DateTime.SpecifyKind(_receivedDate, DateTimeKind.Utc);
        set => _receivedDate = value;
    }

    public bool IsRead { get; set; }

    public bool HasAttachments { get; set; }

    private DateTime? _createTime;
    public DateTime? CreateTime
    {
        get => _createTime.HasValue ? DateTime.SpecifyKind(_createTime.Value, DateTimeKind.Utc) : null;
        set => _createTime = value;
    }
}
