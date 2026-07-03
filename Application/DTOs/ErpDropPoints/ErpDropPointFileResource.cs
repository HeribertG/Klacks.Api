// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpDropPointFileResource
{
    public string Key { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastModifiedUtc { get; set; }

    public string? ErrorReason { get; set; }
}
