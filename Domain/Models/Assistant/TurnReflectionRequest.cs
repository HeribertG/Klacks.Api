// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One occasion to reflect on. Kept deliberately generic so a new trigger only has to describe what
/// went wrong instead of requiring its own service or its own storage path.
/// </summary>
/// <param name="AgentId">Agent whose memory the lesson belongs to</param>
/// <param name="Trigger">Which signal fired, from ReflectionTriggers; carried into the stored metadata</param>
/// <param name="UserMessage">What the user asked, so the lesson can name the situation it applies to</param>
/// <param name="WhatWentWrong">Factual description of the failure, never an interpretation</param>
/// <param name="ScopeKey">What the lesson is about, normally a skill name; this is the memory key and keeps the lesson from applying everywhere</param>
/// <param name="UserId">Optional user the turn belonged to, for provenance only</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record TurnReflectionRequest(
    Guid AgentId,
    string Trigger,
    string UserMessage,
    string WhatWentWrong,
    string ScopeKey,
    Guid? UserId);
