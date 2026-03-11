// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillGapRecord : BaseEntity
{
    public Guid AgentId { get; set; }

    public string UserMessage { get; set; } = string.Empty;

    public string DetectedIntent { get; set; } = string.Empty;

    public int OccurrenceCount { get; set; } = 1;

    public string? SuggestedSkillName { get; set; }

    public string? SuggestedDescription { get; set; }

    public string Status { get; set; } = SkillGapStatuses.Detected;

    public DateTime FirstDetectedAt { get; set; }

    public DateTime LastDetectedAt { get; set; }

    public float[]? Embedding { get; set; }
}
