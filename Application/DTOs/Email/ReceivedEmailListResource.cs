// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

public class ReceivedEmailListResource
{
    public Guid Id { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string ToAddress { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    private DateTime _receivedDate;
    public DateTime ReceivedDate
    {
        get => DateTime.SpecifyKind(_receivedDate, DateTimeKind.Utc);
        set => _receivedDate = value;
    }

    public bool IsRead { get; set; }

    public bool HasAttachments { get; set; }
}
