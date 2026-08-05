// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reserved agent id of the grounding sentinel probe. Findings and counters written under this id
/// are synthetic self-monitoring data: they must be excluded from precision labeling and must
/// never feed the reflection lesson path.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AnswerGroundingSentinel
{
    public static readonly Guid AgentId = new("5e171ee1-0000-4000-8000-000000000001");
}
