// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Expires goal candidates whose underlying observation has stopped occurring. Without this a
/// candidate stays in the inbox until a human decides it, so a proposal keeps being offered long
/// after the condition it was drawn from is gone.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGoalCandidateRevalidationService
{
    /// <summary>Returns how many candidates were expired in this pass.</summary>
    Task<int> RunRevalidationCycleAsync(CancellationToken cancellationToken = default);
}
