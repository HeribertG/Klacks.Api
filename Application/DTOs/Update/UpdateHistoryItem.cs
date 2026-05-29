// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Update;

public class UpdateHistoryItem
{
    public Guid Id { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;

    public string FromVersion { get; set; } = string.Empty;

    public string TargetVersion { get; set; } = string.Empty;

    public bool ContainsMigrations { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Message { get; set; }
}
