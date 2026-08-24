// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the condition-ledger visibility scope (Etappe 3g context block) for a single, explicit user
/// id - never an ambient "current request user" - so it works identically from a live chat turn and from
/// a background replay/evaluation harness that may run for a user other than the one in the current
/// HttpContext.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentConditionScopeResolver
{
    Task<AgentConditionVisibilityScope> ResolveAsync(string userId, CancellationToken cancellationToken = default);
}
