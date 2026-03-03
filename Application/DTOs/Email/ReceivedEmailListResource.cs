// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class ReceivedEmailListResource
{
    public Guid Id { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string ToAddress { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTime ReceivedDate { get; set; }

    public bool IsRead { get; set; }

    public bool HasAttachments { get; set; }
}
