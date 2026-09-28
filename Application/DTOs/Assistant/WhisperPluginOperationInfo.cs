// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class WhisperPluginOperationInfo
{
    public Guid Id { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ModelAlias { get; set; }

    public string? Message { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
