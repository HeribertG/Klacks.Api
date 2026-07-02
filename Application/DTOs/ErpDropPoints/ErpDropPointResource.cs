// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpDropPointResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SourceSystemId { get; set; } = string.Empty;

    public string BucketPrefix { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastPolledAt { get; set; }

    public string? LastError { get; set; }
}
