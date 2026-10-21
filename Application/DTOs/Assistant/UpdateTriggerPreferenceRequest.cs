// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class UpdateTriggerPreferenceRequest
{
    public bool? Muted { get; set; }

    public DateTime? SnoozedUntilUtc { get; set; }

    public string? MinimumSeverity { get; set; }
}
