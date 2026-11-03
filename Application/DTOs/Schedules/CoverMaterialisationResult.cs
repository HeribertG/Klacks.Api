// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a cover wrote into its scenario.
/// </summary>
/// <param name="MovedWorkIds">The scenario works that moved</param>
/// <param name="RelocationsKeepingExtras">Swap relocations the checks counted as moved that stayed a replacement, so their extras stay with the previous agent</param>
/// <param name="HeldContainers">Covers of a container that could not move as a whole (its recalculation failed); a container takes no replacement, so they stay uncovered</param>

using Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record CoverMaterialisationResult(
    IReadOnlySet<Guid> MovedWorkIds,
    IReadOnlyList<CellDelta> RelocationsKeepingExtras,
    IReadOnlyList<CellDelta> HeldContainers);
