// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pre-execution gate that decides whether a skill invocation may run at the user's
/// autonomy level. Returns null when execution is allowed; otherwise a Confirmation
/// SkillResult carrying a one-time token the caller must replay after user approval.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAutonomyGate
{
    Task<SkillResult?> CheckAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}
