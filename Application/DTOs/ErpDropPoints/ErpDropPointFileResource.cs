// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpDropPointFileResource
{
    public string Key { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime LastModifiedUtc { get; set; }

    public string? ErrorReason { get; set; }
}
