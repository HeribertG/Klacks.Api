// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pre-execution gate for the scenario scope of a skill invocation: when the invocation selects a scenario (the
/// "analyseToken" parameter) the caller cannot see, the skill must not run and the token is reported as not found.
/// Returns null when execution is allowed - including a missing or malformed token, which the skill reports itself.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillScenarioScopeGuard
{
    Task<SkillResult?> CheckAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
