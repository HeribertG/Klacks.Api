// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Builds the localized, per-group unique name of a scenario the server creates on its own.
/// </summary>
public interface IScenarioNameGenerator
{
    /// <param name="kind">Which server-side process creates the scenario; selects the localized prefix</param>
    /// <param name="from">First calendar day of the scenario period</param>
    /// <param name="until">Last calendar day of the scenario period (inclusive)</param>
    /// <param name="groupId">Group whose existing scenario names the new name must not collide with</param>
    /// <param name="language">The planner's language; null, blank or unknown falls back to the installation language</param>
    /// <param name="cancellationToken">Cancels the name lookup</param>
    Task<string> GenerateAsync(
        ScenarioNameKind kind,
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        string? language,
        CancellationToken cancellationToken);
}
