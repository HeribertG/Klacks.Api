// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The real rows an accept deletes, selected from the database state before anything is staged: the movable works of the
/// scope and the real breaks the day-lock plan does not keep. The absence guard and the delete step use the same selection
/// logic (ScenarioRealPlanReplacer.SelectDeletionAsync), evaluated by the guard and again by the delete step; it is not one
/// shared result.
/// </summary>
/// <param name="ClientIds">Real clients of the accept scope, or null for every client</param>
/// <param name="Plan">Day-lock plan of the accept</param>
/// <param name="Works">Real works the accept deletes</param>
/// <param name="Breaks">Real breaks the accept deletes (absences and pauses of deleted works)</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record RealPlanDeletion(
    List<Guid>? ClientIds,
    AcceptDayLockPlan Plan,
    IReadOnlyList<Work> Works,
    IReadOnlyList<Break> Breaks);
