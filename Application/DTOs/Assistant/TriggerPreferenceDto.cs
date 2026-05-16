// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class TriggerPreferenceDto
{
    public string TriggerKind { get; set; } = string.Empty;

    public bool Muted { get; set; }

    public DateTime? SnoozedUntilUtc { get; set; }

    public string MinimumSeverity { get; set; } = "low";
}

public class UpdateTriggerPreferenceRequest
{
    public bool? Muted { get; set; }

    public DateTime? SnoozedUntilUtc { get; set; }

    public string? MinimumSeverity { get; set; }
}
