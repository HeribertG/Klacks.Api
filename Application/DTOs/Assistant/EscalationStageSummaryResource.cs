// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class EscalationStageSummaryResource
{
    public int Rank { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserDisplayName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? NotifiedAtUtc { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public DateTime? RespondedAtUtc { get; set; }
}
