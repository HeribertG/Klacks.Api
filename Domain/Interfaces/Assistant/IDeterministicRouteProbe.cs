// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers gate G5: which skills would this message guarantee deterministically ON ITS OWN? Only the
/// deterministic layers are probed (keyword/synonym trigger, operator-authored recipe forcing, engine
/// recipe trigger) - never retrieval, and never the recipe engine's semantic fallback, because neither
/// says whether a message routes by itself and both are the expensive half of an assembly.
/// The interface lives in Domain and its implementation in Application: the probe needs the skill cache,
/// and Domain must not depend on Application.
/// Throws on failure rather than degrading to an empty result: an empty result means "does not route
/// alone", which OPENS the correction path, so swallowing an exception here would silently bias every
/// probe failure towards repairing instead of a fresh request. The caller (TurnPreparationService,
/// which owns gates G0-G5 end to end) decides the fail-closed policy for a broken probe.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IDeterministicRouteProbe
{
    Task<IReadOnlyList<string>> GuaranteedSkillNamesAsync(
        Agent? agent,
        IReadOnlyList<string> userRights,
        string message,
        string? language,
        CancellationToken cancellationToken = default);
}
