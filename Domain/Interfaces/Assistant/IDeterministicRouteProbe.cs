// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers gate G5: which skills would this message guarantee deterministically ON ITS OWN? Only the
/// deterministic layers are probed (keyword/synonym trigger, operator-authored recipe forcing, engine
/// recipe trigger) - never retrieval, and never the recipe engine's semantic fallback, because neither
/// says whether a message routes by itself and both are the expensive half of an assembly.
/// The interface lives in Domain and its implementation in Application: the probe needs the skill cache,
/// and Domain must not depend on Application.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IDeterministicRouteProbe
{
    Task<IReadOnlyList<string>> GuaranteedSkillNamesAsync(
        Agent? agent,
        List<string> userRights,
        string message,
        string? language,
        CancellationToken cancellationToken = default);
}
