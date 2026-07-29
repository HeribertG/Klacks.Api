// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public record GoalCandidateDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Rationale { get; init; } = string.Empty;

    public string Confidence { get; init; } = string.Empty;

    public string SignalSource { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? CreatedUtc { get; init; }

    public DateTime? DecidedUtc { get; init; }

    public Guid? PlanId { get; init; }
}
