// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Source of raw signals for Klacksy's self-directed goal reflection (Phase 1: shadow mode).
/// Kept as an interface so the reflection cycle can be pointed at a different origin later — the
/// design spec names AgentMemory facts as a future source, but that source first needs a
/// provenance marker to avoid a self-referential loop (memory -> goal -> plan turns -> new memory
/// -> next reflection reads its own output). Phase 1 therefore uses the deterministic proactive
/// trigger history instead.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoalSignalSource
{
    Task<IReadOnlyList<GoalSignal>> CollectAsync(CancellationToken cancellationToken = default);
}
