// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class EscalationRosterEntryResource
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int? EffectiveRank { get; set; }

    public bool HasOverride { get; set; }

    public bool IsOrphaned { get; set; }
}
