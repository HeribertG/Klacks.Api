// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the live summary of one scenario from its own shifts and works.
/// <c>BuildAsync</c> returns null when no scenario carries the token.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules.Summary;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IScenarioSummaryBuilder
{
    Task<ScenarioSummaryDto?> BuildAsync(Guid token, CancellationToken ct);
}
